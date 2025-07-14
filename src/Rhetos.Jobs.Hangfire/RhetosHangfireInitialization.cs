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
using System.Globalization;
using System.Linq;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// Initializes Hangfire's global configuration, required for both the components that enqueue jobs
    /// and the Hangfire job server that processes the jobs.
    /// </summary>
    public class RhetosHangfireInitialization
	{
		private readonly RhetosJobHangfireOptions _rhetosJobHangfireOptions;

        private static bool _initialized;
		private static readonly object _initializationLock = new();

        public RhetosHangfireInitialization(RhetosJobHangfireOptions rhetosJobHangfireOptions)
        {
            _rhetosJobHangfireOptions = rhetosJobHangfireOptions;
        }

        /// <summary>
        /// Initializes Hangfire's global configuration, if not initialized already,
        /// required for both the components that enqueue jobs and the Hangfire job server that processes the jobs.
        /// </summary>
        /// <remarks>
        /// Call this method before using Hangfire to create background jobs in a CLI utility or unit tests.
        /// This method is automatically called in Rhetos web application startup.
        /// </remarks>
        public virtual void InitializeGlobalConfiguration()
		{
			if (!_initialized)
				lock (_initializationLock)
					if (!_initialized)
					{
                        GlobalConfiguration.Configuration
                            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                            .UseSimpleAssemblyNameTypeSerializer()
                            .UseRecommendedSerializerSettings();

                        // The call to .UseSqlServerStorage(_connectionString, _sqlServerStorageOptions) is removed, so that
                        // there is no global Hangfire storage by default, in order to support multitenant applications with separate database per tenant.
                        // The following lines with UseDashboardMetric are copied from inside UseSqlServerStorage => UseSqlServerStorageCommonMetrics.
                        // The call to UseStorage from UseSqlServerStorage is separated below, it the global connection string is configured.
                        GlobalConfiguration.Configuration
                            .UseDashboardMetric(SqlServerStorage.SchemaVersion)
                            .UseDashboardMetric(SqlServerStorage.ActiveConnections)
                            .UseDashboardMetric(SqlServerStorage.TotalConnections)
                            .UseDashboardMetric(SqlServerStorage.ActiveTransactions)
                            .UseDashboardMetric(SqlServerStorage.DataFilesSize)
                            .UseDashboardMetric(SqlServerStorage.LogFilesSize);

                        var automaticRetryAttribute = new AutomaticRetryAttribute { Attempts = _rhetosJobHangfireOptions.AutomaticRetryAttempts };
						if (!string.IsNullOrEmpty(_rhetosJobHangfireOptions.DelaysInSeconds))
							automaticRetryAttribute.DelaysInSeconds = ParseDelaysInSeconds();

						GlobalJobFilters.Filters.Add(automaticRetryAttribute);

#pragma warning disable S2696 // Instance members should not write to "static" fields. This is a standard double-checked locking.
						_initialized = true;
#pragma warning restore S2696 // Instance members should not write to "static" fields
                    }
		}

        /// <summary>
        /// This method is intended for regular apps with a global connection string, in order to
        /// simplify Hangfire Dashboard initialization.
        /// In a multitenant app with separate database per tenant, the Hangfire Dashboard
        /// needs to be initialized separately for each database, by using <see cref="JobStorageCollection"/>
        /// to get the job storage for each database/tenant, instead of calling this methods.
        /// </summary>
        public virtual void InitializeGlobalJobStorage(JobStorage globalJobStorage)
        {
            GlobalConfiguration.Configuration.UseStorage(globalJobStorage);
        }

        private int[] ParseDelaysInSeconds() =>
            _rhetosJobHangfireOptions.DelaysInSeconds.Split(',').Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();
    }
}
