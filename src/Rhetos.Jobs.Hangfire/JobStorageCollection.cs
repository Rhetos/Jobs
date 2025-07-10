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
using Hangfire.SqlServer;
using Rhetos.Utilities;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// A singleton that keeps track of Hangfire job databases, instead of the Hangfire's static global configuration.
    /// The Hangfire's global configuration does not support applications that do not have a *global* <see cref="ConnectionString"/> (for example, a multitenant app with database per tenant).
    /// </summary>
    /// <remarks>
    /// For application code in the scope of the web request, use <see cref="JobStorageProvider"/> instead of <see cref="JobStorageCollection"/>.
    /// Use <see cref="JobStorageCollection"/> directly in the application initialization code, where a global connection string might not be available (in a multitenant app with database per tenant).
    /// </remarks>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix. JobStorageCollection represents a "collection", but it uses composition instead of inheritance.
    public class JobStorageCollection
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        private readonly RhetosJobHangfireOptions _rhetosJobHangfireOptions;
        private readonly RhetosHangfireInitialization _rhetosHangfireInitialization;
        private readonly ISqlUtility _sqlUtility;
        private readonly ConcurrentDictionary<string, JobStorage> _storages = new();
        private readonly ConcurrentDictionary<string, RecurringJobManager> _recurringJobManagers = new();
        private readonly ConcurrentDictionary<string, BackgroundJobClient> _backgroundJobClients = new();
        private readonly SqlServerStorageOptions _sqlServerStorageOptions;

        public JobStorageCollection(RhetosJobHangfireOptions rhetosJobHangfireOptions, RhetosHangfireInitialization rhetosHangfireInitialization, ISqlUtility sqlUtility)
        {
            _rhetosJobHangfireOptions = rhetosJobHangfireOptions;
            _rhetosHangfireInitialization = rhetosHangfireInitialization;
            _sqlUtility = sqlUtility;
            _sqlServerStorageOptions = new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromSeconds(rhetosJobHangfireOptions.CommandBatchMaxTimeout),
                SlidingInvisibilityTimeout = TimeSpan.FromSeconds(rhetosJobHangfireOptions.SlidingInvisibilityTimeout),
                QueuePollInterval = TimeSpan.FromSeconds(rhetosJobHangfireOptions.QueuePollInterval),
                UseRecommendedIsolationLevel = rhetosJobHangfireOptions.UseRecommendedIsolationLevel,
                DisableGlobalLocks = rhetosJobHangfireOptions.DisableGlobalLocks
            };
        }

        public JobStorage GetStorage(string connectionString)
        {
            return _storages.GetOrAdd(connectionString, CreateStorage);
        }

        private JobStorage CreateStorage(string connectionString)
        {
            _rhetosHangfireInitialization.InitializeGlobalConfiguration();
            return new SqlServerStorage(GetConnectionStringWithAppName(connectionString), _sqlServerStorageOptions);
        }

        public RecurringJobManager GetRecurringJobManager(string connectionString)
        {
            return _recurringJobManagers.GetOrAdd(connectionString, CreateRecurringJobManager);
        }

        private RecurringJobManager CreateRecurringJobManager(string connectionString)
        {
            var jobStorage = GetStorage(connectionString);
            return new RecurringJobManager(jobStorage);
        }

        public BackgroundJobClient GetBackgroundJobClient(string connectionString)
        {
            return _backgroundJobClients.GetOrAdd(connectionString, CreateBackgroundJobClient);
        }

        private BackgroundJobClient CreateBackgroundJobClient(string connectionString)
        {
            var jobStorage = GetStorage(connectionString);
            return new BackgroundJobClient(jobStorage);
        }

        private string GetConnectionStringWithAppName(string connectionString)
        {
            if (_rhetosJobHangfireOptions.SetConnectionStringApplicationName)
            {
                string hostAppName = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Rhetos";
                string dbApplicationName = $"{hostAppName} Hangfire";
                return _sqlUtility.SetApplicationName(connectionString, dbApplicationName);
            }
            else
                return connectionString;
        }
    }
}