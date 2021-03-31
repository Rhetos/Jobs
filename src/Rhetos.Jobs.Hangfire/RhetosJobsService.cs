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

			yield return new BackgroundJobServer();
		}
	}
}
