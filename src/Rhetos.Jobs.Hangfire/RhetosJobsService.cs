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

		public RhetosJobsService(ConnectionString connectionString)
		{
			_connectionString = connectionString;
		}

		public void Initialize()
		{
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
