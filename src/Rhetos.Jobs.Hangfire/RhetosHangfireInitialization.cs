using Autofac;
using Hangfire;
using Hangfire.SqlServer;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;

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

		private static bool _initialized = false;
		private static readonly object _initializationLock = new object();

		public RhetosHangfireInitialization(ConnectionString connectionString, RhetosJobHangfireOptions options)
		{
			_connectionString = connectionString;
			_options = options;
		}

        /// <summary>
        /// Initializes Hangfire's global configuration, if not initialized already.
        /// </summary>
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

						_initialized = true;
					}
		}

        public IEnumerable<IDisposable> GetHangfireServers(ILifetimeScope container)
        {
	        GlobalConfiguration.Configuration.UseAutofacActivator(container);
	        yield return new BackgroundJobServer(new BackgroundJobServerOptions
	        {
		        WorkerCount = _options.WorkerCount,
		        ShutdownTimeout = TimeSpan.FromSeconds(_options.ShutdownTimeout),
		        StopTimeout = TimeSpan.FromSeconds(_options.StopTimeout),
		        SchedulePollingInterval = TimeSpan.FromSeconds(_options.SchedulePollingInterval),
		        HeartbeatInterval = TimeSpan.FromSeconds(_options.HeartbeatInterval),
		        ServerTimeout = TimeSpan.FromSeconds(_options.ServerTimeout),
		        ServerCheckInterval = TimeSpan.FromSeconds(_options.ServerCheckInterval),
		        CancellationCheckInterval = TimeSpan.FromSeconds(_options.CancellationCheckInterval),
	        });
        }

	}
}
