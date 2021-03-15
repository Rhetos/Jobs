using System;
using System.Collections.Generic;
using Autofac.Integration.Wcf;
using Hangfire;
using Hangfire.SqlServer;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos.Jobs
{

	public class RhetosJobsService : IService
	{
		private readonly ConnectionString _connectionString;
		private readonly ILogger _logger;
		public RhetosJobsService(ILogProvider logProvider, ConnectionString connectionString)
		{
			_connectionString = connectionString;
			_logger = logProvider.GetLogger("TheService");
		}

		public void Initialize()
		{
			_logger.Trace("RhetosJobsService initalized");
		}

		public void InitializeApplicationInstance(System.Web.HttpApplication context)
		{
			GlobalConfiguration.Configuration.UseAutofacActivator(AutofacHostFactory.Container);
			HangfireAspNet.Use(GetHangfireServers);
		}

		private IEnumerable<IDisposable> GetHangfireServers()
		{
			GlobalConfiguration.Configuration
				.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
				.UseSimpleAssemblyNameTypeSerializer()
				.UseRecommendedSerializerSettings()
				.UseSqlServerStorage(_connectionString, new SqlServerStorageOptions
				{
					CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
					SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
					QueuePollInterval = TimeSpan.Zero,
					UseRecommendedIsolationLevel = true,
					UsePageLocksOnDequeue = true,
					DisableGlobalLocks = true
				});

			yield return new BackgroundJobServer();
		}
	}
}
