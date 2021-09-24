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

namespace Rhetos.Jobs.Hangfire
{
	/// <summary>
	/// Initializes Hangfire's global configuration, required for both the components that enqueue jobs
	/// and the Hangfire job server that processes the jobs.
	/// </summary>
	public class RhetosHangfireInitialization
	{
		private readonly ConnectionString _connectionString;
		private readonly RhetosJobHangfireOptions _options;

		private static bool _initialized;
		private static readonly object _initializationLock = new();

		public RhetosHangfireInitialization(ConnectionString connectionString, RhetosJobHangfireOptions options)
		{
			_connectionString = connectionString;
			_options = options;
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
							.UseRecommendedSerializerSettings()
							.UseSqlServerStorage(_connectionString, new SqlServerStorageOptions
							{
								CommandBatchMaxTimeout = TimeSpan.FromSeconds(_options.CommandBatchMaxTimeout),
								SlidingInvisibilityTimeout = TimeSpan.FromSeconds(_options.SlidingInvisibilityTimeout),
								QueuePollInterval = TimeSpan.FromSeconds(_options.QueuePollInterval),
								UseRecommendedIsolationLevel = _options.UseRecommendedIsolationLevel,
								DisableGlobalLocks = _options.DisableGlobalLocks
							});

#pragma warning disable S2696 // Instance members should not write to "static" fields. This is a standard double-checked locking.
						_initialized = true;
#pragma warning restore S2696 // Instance members should not write to "static" fields
                    }
		}
	}
}
