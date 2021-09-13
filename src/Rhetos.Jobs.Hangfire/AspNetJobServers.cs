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

using Hangfire;
using Rhetos.Logging;
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
    /// In other processes, for example CLI utilities or unit tests, instead of this class, use <see cref="RhetosJobServer"/> directly to create
    /// the Hangfire <see cref="BackgroundJobServer"/>, in order to start job processing in the current application.
    /// </remarks>
    public class AspNetJobServers : IDisposable
    {
        private readonly RhetosJobHangfireOptions _options;
        private readonly RhetosJobServer _rhetosJobServer;
        private readonly ILogger _logger;
        private bool disposed;

        /// <summary>
        /// List of created job servers that will be shutdown when host is closing.
        /// </summary>
        public ConcurrentBag<BackgroundJobServer> Servers { get; private set; } = new();

        public AspNetJobServers(RhetosJobHangfireOptions options, RhetosJobServer rhetosJobServer, ILogProvider logProvider)
        {
            _options = options;
            _rhetosJobServer = rhetosJobServer;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Creates a new instance of the Hangfire jobs server that runs in background,
        /// and registers it for disposal when Rhetos DI closes.
        /// </summary>
        /// <remarks>
        /// Before creating job servers, initialize global settings with <see cref="RhetosJobServer.Initialize"/>.
        /// The Hangfire job server will not be created if <see cref="RhetosJobHangfireOptions.InitializeHangfireServer"/>
        /// is set to <see langword="false"/>.
        /// </remarks>
        public void CreateJobServer()
        {
            if (_options.InitializeHangfireServer)
            {
                var jobServer = _rhetosJobServer.CreateHangfireJobServer();
                Servers.Add(jobServer);
            }
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

            var servers = new List<BackgroundJobServer>();
            while (Servers.TryTake(out var server))
                servers.Add(server);

            if (servers.Any())
                _logger.Trace($"{nameof(ShutdownJobServers)} started. {servers.Count} job servers.");

            // Sending stop signals to all servers at once. This will avoid waiting for each independently (from Hangfire documentation).
            foreach (var server in servers)
                try
                {
                    server.SendStop();
                }
                catch
                {
                    // Ignoring possible issues if any server is already disposed on shutdown.
                }

            // Waiting for each server to stop.
            foreach (var server in servers)
                try
                {
                    server.Dispose();
                }
                catch
                {
                    // Ignoring possible issues if any server is already disposed on shutdown.
                }

            if (servers.Any())
                _logger.Trace($"{nameof(ShutdownJobServers)} finished.");
        }
    }
}