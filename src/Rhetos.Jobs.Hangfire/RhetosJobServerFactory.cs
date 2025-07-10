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
using System;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// Initializes Hangfire server for background job processing in a Rhetos applications.
    /// </summary>
    /// <remarks>
    /// This class is intended for direct control over Hangfire job server *lifetime* in unit tests, CLI utilities or Windows services.
    /// For simpler usage alternative see <see cref="JobServersCollection"/> or <see cref="RhetosJobsHangfireStartupExtensions.UseRhetosHangfireServer(Microsoft.AspNetCore.Builder.IApplicationBuilder, Action{BackgroundJobServerOptions}[])"/>.
    /// </remarks>
    public class RhetosJobServerFactory
    {
        private readonly RhetosJobHangfireOptions _options;
        private readonly JobStorageCollection _jobStorageCollection;

        public RhetosJobServerFactory(RhetosJobHangfireOptions options, JobStorageCollection jobStorageCollection)
        {
            _options = options;
            _jobStorageCollection = jobStorageCollection;
        }

        /// <summary>
        /// Creates a new instance of Hangfire <see cref="BackgroundJobServer"/> that runs in background.
        /// It uses application-specific configuration from <see cref="RhetosJobHangfireOptions"/> for <see cref="BackgroundJobServerOptions"/>.
        /// </summary>
        /// <remarks>
        /// The Hangfire BackgroundJobServer will start processing background jobs immediately.
        /// <para>
        /// Before creating job server, the Hangfire's GlobalConfiguration must be configured to use Rhetos host DI container,
        /// by calling <code>GlobalConfiguration.Configuration.UseAutofacActivator(rhetosHost.GetRootContainer());</code>
        /// </para>
        /// </remarks>
        /// <param name="configureOptions">
        /// Overrides configuration loaded form app settings.
        /// </param>
        public BackgroundJobServer CreateHangfireJobServer(string connectionString, Action<BackgroundJobServerOptions> configureOptions = null)
        {
            var hangfireOptions = new BackgroundJobServerOptions
            {
                ServerName = $"{Environment.MachineName}.{AppDomain.CurrentDomain.FriendlyName}",
                WorkerCount = _options.WorkerCount,
                ShutdownTimeout = TimeSpan.FromSeconds(_options.ShutdownTimeout),
                StopTimeout = TimeSpan.FromSeconds(_options.StopTimeout),
                SchedulePollingInterval = TimeSpan.FromSeconds(_options.SchedulePollingInterval),
                HeartbeatInterval = TimeSpan.FromSeconds(_options.HeartbeatInterval),
                ServerTimeout = TimeSpan.FromSeconds(_options.ServerTimeout),
                ServerCheckInterval = TimeSpan.FromSeconds(_options.ServerCheckInterval),
                CancellationCheckInterval = TimeSpan.FromSeconds(_options.CancellationCheckInterval),
                Queues = _options.Queues,
            };

            configureOptions?.Invoke(hangfireOptions);

            JobStorage jobStorage = _jobStorageCollection.GetStorage(connectionString);
            return new BackgroundJobServer(hangfireOptions, jobStorage);
        }
    }
}
