using System;
using System.Data;
using System.Data.SqlClient;
using Autofac;
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
		private readonly ConnectionString _connectionString;
		private readonly ILogger _logger;

		public JobExecuter(ILogProvider logProvider, ConnectionString connectionString)
		{
			_connectionString = connectionString;
			_logger = logProvider.GetLogger("RhetosJobs");
		}

		public void ExecuteJob(IJob job)
		{
			if (!JobExists(job.Id, _connectionString))
				return;

			using (var scope = new ProcessContainer().CreateTransactionScopeContainer(builder => CustomizeScope(builder, job.ExecuteAsUser)))
			{
				var actions = scope.Resolve<INamedPlugins<IActionRepository>>();
				var actionType = scope.Resolve<IDomainObjectModel>().GetType(job.ActionName);
				var actionRepository = actions.GetPlugin(job.ActionName);
				var parameters = JsonConvert.DeserializeObject(job.ActionParameters, actionType);
				actionRepository.Execute(parameters);

				scope.CommitChanges();
			}
		}

		private static bool JobExists(Guid jobId, string connectionString)
		{
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var command = new SqlCommand($"SELECT * FROM RhetosJobs.Job WITH (READCOMMITTEDLOCK, ROWLOCK) WHERE ID = '{jobId}'", connection);
				var reader = command.ExecuteReader();
				var table = new DataTable();
				table.Load(reader);

				//If transaction that created job failed there will be no job scheduled and count will be zero
				return table.Rows.Count > 0;
			}
		}

		private void CustomizeScope(ContainerBuilder builder, string userName = null)
		{
			if (string.IsNullOrWhiteSpace(userName))
				builder.RegisterType(typeof(ProcessUserInfo)).As<IUserInfo>();
			else
				builder.RegisterInstance(new JobExecuterUserInfo(userName)).As<IUserInfo>();
		}

		class JobExecuterUserInfo : IUserInfo
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