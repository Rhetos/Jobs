using System;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos.Jobs
{
	public class JobExecuter : IJobExecuter
	{
		private readonly IDomainObjectModel _domainObjectModel;
		private readonly INamedPlugins<IActionRepository> _actionPlugins;
		private readonly ConnectionString _connectionString;
		private readonly ILogger _logger;

		public JobExecuter(ILogProvider logProvider, IDomainObjectModel domainObjectModel, INamedPlugins<IActionRepository> actionPlugins, ConnectionString connectionString)
		{
			_domainObjectModel = domainObjectModel;
			_actionPlugins = actionPlugins;
			_connectionString = connectionString;
			_logger = logProvider.GetLogger("RhetosJobs");
		}

		public void ExecuteJob(Guid jobId, string actionName, string actionParameters)
		{
			_logger.Info("ExecuteTask started");

			if (!JobExists(jobId, _connectionString))
				return;

			_logger.Info("ExecuteTask resolving task ");

			var actionType = _domainObjectModel.GetType(actionName);
			var actionRepository = _actionPlugins.GetPlugin(actionName);
			var parameters = JsonConvert.DeserializeObject(actionParameters, actionType);
			actionRepository.Execute(parameters);
		}

		private static bool JobExists(Guid jobId, string connectionString)
		{
			var connection = new SqlConnection(connectionString);
			connection.Open();
			var command = new SqlCommand($"SELECT * FROM RhetosJobs.Job WITH (READCOMMITTEDLOCK, ROWLOCK) WHERE ID = '{jobId}'", connection);
			var reader = command.ExecuteReader();
			var table = new DataTable();
			table.Load(reader);

			//If tranasaction that created job failed there will be no job scheduled and count will be zero
			return table.Rows.Count > 0;
		}
	}
}