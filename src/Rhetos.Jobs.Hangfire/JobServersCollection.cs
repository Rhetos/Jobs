/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Autofac;
using Hangfire;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// A singleton that keeps track of Hangfire job servers that have been created by Rhetos Hangfire integration,
    /// in order to stop them on dispose.
    /// </summary>
    /// <remarks>
    /// For manual control over Hangfire job server lifetime, use <see cref="RhetosJobServerFactory"/> instead.
    /// For simpler usage in web apps, see <see cref="RhetosJobsHangfireStartupExtensions.UseRhetosHangfireServer(Microsoft.AspNetCore.Builder.IApplicationBuilder, Action{BackgroundJobServerOptions}[])"/> instead.
    /// </remarks>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix. JobServersCollection represents a "collection", but it uses composition instead of inheritance.
    public class JobServersCollection : IDisposable
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        private readonly RhetosJobHangfireOptions _options;
        private readonly RhetosJobServerFactory _rhetosJobServerFactory;
        private readonly ILogger _logger;
        private bool disposed;

        /// <summary>
        /// List of created job servers that will be shutdown when host is closing.
        /// </summary>
        public ConcurrentBag<(BackgroundJobServer BackgroundJobServer, ILifetimeScope OwnedLifetimeScope)> Servers { get; private set; } = [];

        public JobServersCollection(RhetosJobHangfireOptions options, RhetosJobServerFactory rhetosJobServerFactory, ILogProvider logProvider)
        {
            _options = options;
            _rhetosJobServerFactory = rhetosJobServerFactory;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Creates a new instance of the Hangfire jobs server that runs in background,
        /// and registers it for disposal when Rhetos DI closes.
        /// </summary>
        /// <remarks>
        /// The Hangfire job server will not be created if option <see cref="RhetosJobHangfireOptions.InitializeHangfireServer"/>
        /// is set to <see langword="false"/>.
        /// </remarks>
        /// <param name="ownLifetimeScope">
        /// Set to true if the lifetime scope is created specifically for the job server, and <see cref="JobServersCollection"/> should dispose the lifetime scope when disposing the container.
        /// </param>
        public void CreateJobServer(ILifetimeScope lifetimeScope, Action<BackgroundJobServerOptions> configureOptions = null, bool ownLifetimeScope = false)
        {
            if (_options.InitializeHangfireServer)
            {
                var jobServer = _rhetosJobServerFactory.CreateHangfireJobServer(lifetimeScope, configureOptions);
                Servers.Add((jobServer, ownLifetimeScope ? lifetimeScope : null));
            }
        }

        /// <summary>
        /// Creates a new instance of the Hangfire jobs server that runs in background,
        /// and registers it for disposal when Rhetos DI closes.
        /// </summary>
        /// <remarks>
        /// The Hangfire job server will not be created if option <see cref="RhetosJobHangfireOptions.InitializeHangfireServer"/>
        /// is set to <see langword="false"/>.
        /// </remarks>
        /// <param name="configureOptions">
        /// The configuration parameter may be <see langword="null"/>; it uses app setting for <see cref="RhetosJobHangfireOptions"/> by default.
        /// </param>
        /// <param name="connectionString">
        /// Connection string should be <see langword="null"/>, if there is global connection string available for the application.
        /// If the connection string is specified, the job server will be created in a separate lifetime scope with custom connection string.
        /// Use this for multitenant apps with separate database per tenant.
        /// </param>
        public void CreateJobServer(RhetosHost rhetosHost, Action<BackgroundJobServerOptions> configureOptions = null, string connectionString = null)
        {
            ILifetimeScope rootContainer = rhetosHost.GetRootContainer();
            ILifetimeScope jobServerLifetimeScope;
            if (connectionString != null)
            {
                var unitOfWorkFactory = new UnitOfWorkFactory();
                jobServerLifetimeScope = rootContainer.BeginLifetimeScope(builder =>
                {
                    builder.RegisterInstance(new ConnectionString(connectionString));
                    builder.RegisterInstance(unitOfWorkFactory).As<UnitOfWorkFactory>().As<IUnitOfWorkFactory>();
                });
                unitOfWorkFactory.Initialize(jobServerLifetimeScope);
            }
            else
            {
                jobServerLifetimeScope = rootContainer;
                if (RhetosJobsHangfireStartupExtensions.TryGetGlobalConnectionString(rootContainer) == null)
                    throw new ArgumentException($"The connection string is not specified in this method call, and there is no global connection string available.");
            }

            CreateJobServer(jobServerLifetimeScope, configureOptions, ownLifetimeScope: jobServerLifetimeScope != rootContainer);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ShutdownJobServers();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops all Hangfire jobs servers that were created by this class.
        /// This method will wait for currently running jobs to complete for duration of <see cref="RhetosJobHangfireOptions.ShutdownTimeout"/>,
        /// then return anyway if not completed (see Hangfire documentation on ShutdownTimeout).
        /// </summary>
        public void ShutdownJobServers()
        {
            // This method may be removed after we migrate to direct usage of Hangfire ASP.NET integration,
            // because then Hangfire will take case of shutting down job servers on disposal.

            if (Servers == null)
                return;

            var servers = new List<(BackgroundJobServer BackgroundJobServer, ILifetimeScope OwnedLifetimeScope)>();
            while (Servers.TryTake(out var server))
                servers.Add(server);

            if (servers.Count != 0)
                _logger.Trace($"{nameof(ShutdownJobServers)} started. {servers.Count} job servers.");

            // Sending stop signals to all servers at once. This will avoid waiting for each independently (from Hangfire documentation).
            TryForeach(servers, server => server.BackgroundJobServer.SendStop());

            // Waiting for each server to stop.
            TryForeach(servers, server => server.BackgroundJobServer.Dispose());

            // Dispose any lifetime scopes that were created specifically for the job server, and not managed outside of the job server collection.
            TryForeach(servers.Select(s => s.OwnedLifetimeScope).Where(scope => scope != null), ownedLifetimeScope => ownedLifetimeScope.Dispose());

            if (servers.Count != 0)
                _logger.Trace($"{nameof(ShutdownJobServers)} finished.");
        }

        /// <summary>
        /// Executes the action on each of the objects in the list. It ignores any exceptions and continues with other objects in the list.
        /// </summary>
        private static void TryForeach<T>(IEnumerable<T> objects, Action<T> action)
        {
            foreach (var o in objects)
#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    action.Invoke(o);
                }
                catch
                {
                    // Ignoring possible issues if any server is already disposed on shutdown, or other disposal issues.
                    // The server and scope disposal is usually happening on application shutdown.
                }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}