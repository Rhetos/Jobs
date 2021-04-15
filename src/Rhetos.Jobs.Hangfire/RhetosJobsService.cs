using System;
using System.Collections.Generic;
using Autofac.Integration.Wcf;
using Hangfire;
using Hangfire.SqlServer;
using Rhetos.Utilities;

namespace Rhetos.Jobs.Hangfire
{
	public class RhetosJobsService : IService
	{
		private readonly ConnectionString _connectionString;
		private readonly RhetosJobHangfireOptions _options;
		public RhetosJobsService(ConnectionString connectionString, RhetosJobHangfireOptions options)
		{
			_connectionString = connectionString;
			_options = options;
		}

		public void Initialize()
		{
			GlobalConfiguration.Configuration.UseAutofacActivator(AutofacHostFactory.Container);
			HangfireAspNet.Use(GetHangfireServers);
		}

		public void InitializeApplicationInstance(System.Web.HttpApplication context)
		{
		}

		private IEnumerable<IDisposable> GetHangfireServers()
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
