using System;
using Autofac;
using Autofac.Integration.Wcf;
using Newtonsoft.Json;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;

namespace Rhetos.Jobs.Hangfire
{
	public class JobExecuter : IJobExecuter
	{
		private readonly ILogger _logger;
		private readonly RhetosJobHangfireOptions _options;

		public JobExecuter(ILogProvider logProvider, RhetosJobHangfireOptions options)
		{
			// Note: Constructor parameters are resolved from the root DI container (set in UseAutofacActivator call in RhetosJobsService class).
			_logger = logProvider.GetLogger(InternalExtensions.LoggerName);
			_options = options;
		}

		public void ExecuteJob(Job job)
		{
			_logger.Trace(() => $"ExecuteJob started.|{job.GetLogInfo()}");
			
			try
			{
				using (var scope = new TransactionScopeContainer((IContainer) AutofacHostFactory.Container,
					builder => CustomizeScope(builder, job.ExecuteAsUser)))
				{
					_logger.Trace(() => $"ExecuteJob TransactionScopeContainer initialized.|{job.GetLogInfo()}");

					var sqlExecuter = scope.Resolve<ISqlExecuter>();
					if (!JobExists(job.Id, sqlExecuter))
					{
						_logger.Trace(() =>
							$"Job no longer exists in queue. Transaction in which was job created was rollbacked. Terminating execution.|{job.GetLogInfo()}");
						return;
					}

					var actions = scope.Resolve<INamedPlugins<IActionRepository>>();
					var actionType = scope.Resolve<IDomainObjectModel>().GetType(job.ActionName);
					var actionRepository = actions.GetPlugin(job.ActionName);
					var parameters = JsonConvert.DeserializeObject(job.ActionParameters, actionType);
					actionRepository.Execute(parameters);

					DeleteJob(job.Id, sqlExecuter);
					scope.CommitChanges();
				}
			}
			catch (Exception exception)
			{
				_logger.Error($"ExecuteJob exception: {exception}");
				throw;
			}

			_logger.Trace(() => $"ExecuteJob completed.|{job.GetLogInfo()}");
		}

		private static bool JobExists(Guid jobId, ISqlExecuter sqlExecuter)
		{
			var command = $"SELECT COUNT(1) FROM RhetosJobs.HangfireJob WITH (READCOMMITTEDLOCK, ROWLOCK) WHERE ID = '{jobId}'";
			var count = 0;

			sqlExecuter.ExecuteReader(command, reader => count = reader.GetInt32(0));

			//If transaction that created job failed there will be no job scheduled and count will be zero
			return count > 0;
		}

		private static void DeleteJob(Guid jobId, ISqlExecuter sqlExecuter)
		{
			var command = $"DELETE FROM RhetosJobs.HangfireJob WHERE ID = '{jobId}'";
			sqlExecuter.ExecuteSql(command);
		}

		private void CustomizeScope(ContainerBuilder builder, string userName = null)
		{
			if (!string.IsNullOrWhiteSpace(userName))
				builder.RegisterInstance(new JobExecuterUserInfo(userName)).As<IUserInfo>();
			else if (!string.IsNullOrWhiteSpace(_options.ProcessUserName))
				builder.RegisterInstance(new JobExecuterUserInfo(_options.ProcessUserName)).As<IUserInfo>();
			else
				builder.RegisterType(typeof(ProcessUserInfo)).As<IUserInfo>();
		}

		private class JobExecuterUserInfo : IUserInfo
		{
			public JobExecuterUserInfo(string userName)
			{
				UserName = userName;
				IsUserRecognized = true;
				Workstation = "Async job";
			}

			public bool IsUserRecognized { get; }
			public string UserName { get; }
			public string Workstation { get; }

			public string Report() { return UserName + "," + Workstation; }
		}
	}
}