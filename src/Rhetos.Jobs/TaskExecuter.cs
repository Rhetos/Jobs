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

namespace Rhetos.Jobs
{
	public class TaskExecuter
	{
		private readonly ConnectionString _connectionString;

		private readonly ILogger _logger;
		private readonly INamedPlugins<IActionRepository> _actionPlugins;
		private readonly IDomainObjectModel _domainObjectModel;
		// private readonly IContainer _container;

		public TaskExecuter(ConnectionString connectionString, ILogProvider logProvider, INamedPlugins<IActionRepository> actionPlugins, IDomainObjectModel domainObjectModel)//, IContainer container)
		{
			_connectionString = connectionString;
			_logger = logProvider.GetLogger("RhetosJobs");
			_logger.Info("TaskExecuter initalized");

			_actionPlugins = actionPlugins;
			_domainObjectModel = domainObjectModel;
			// _container = container;
		}

		// public void ExecuteTask(Guid? taskId)
		// {
		// 	_logger.Info($"ExecuteTask|{taskId}|Begin");
		//
		// 	var connection = new SqlConnection(_connectionString);
		// 	connection.Open();
		// 	var command = new SqlCommand($"SELECT * FROM RhetosJobs.Task WITH (READCOMMITTEDLOCK, ROWLOCK) WHERE ID = '{taskId}'", connection);
		// 	var reader = command.ExecuteReader();
		// 	var table = new DataTable();
		// 	table.Load(reader);
		//
		// 	var task = _taskRepository.Load(x => x.ID == taskId).FirstOrDefault();
		// 	_logger.Info($"ExecuteTask|{taskId}|Task loaded");
		//
		// 	if (task != null)
		// 		ExecuteTask(task);
		// }

		public void ExecuteTask(Guid taskId, string actionName, string actionParameters)
		{
			_logger.Info("ExecuteTask started");

			var connection = new SqlConnection(_connectionString);
			connection.Open();
			var command = new SqlCommand($"SELECT * FROM RhetosJobs.Task WITH (READCOMMITTEDLOCK, ROWLOCK) WHERE ID = '{taskId}'", connection);
			var reader = command.ExecuteReader();
			var table = new DataTable();
			table.Load(reader);
			
			if(table.Rows.Count == 0)
				return;

			_logger.Info("ExecuteTask resolving task ");

			var actionType = _domainObjectModel.GetType(actionName);
			var actionRepository = _actionPlugins.GetPlugin(actionName);
			var parameters = JsonConvert.DeserializeObject(actionParameters, actionType);
			actionRepository.Execute(parameters);

			_logger.Info("ExecuteTask task executed");

			// using (var scope = new TransactionScopeContainer(_container, CustomizeScope))
			// {
			// 	var actions = scope.Resolve<INamedPlugins<IActionRepository>>();
			// 	var actionType = scope.Resolve<IDomainObjectModel>().GetType(task.Name);
			// 	var actionRepository = actions.GetPlugin(task.Name);
			// 	var parameters = JsonConvert.DeserializeObject(task.Parameters, actionType);
			// 	actionRepository.Execute(parameters);
			//
			// 	scope.CommitChanges();
			// }
		}

		// private void CustomizeScope(ContainerBuilder builder)
		// {
		// 	builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
		// }
	}
}