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
	public interface ITaskExecuter
	{
		void ExecuteTask(Guid taskId, string actionName, string actionParameters);
	}

	public class TaskExecuter : ITaskExecuter
	{
		private readonly ConnectionString _connectionString;
		private readonly ILogProvider _logProvider;
		private readonly ILogger _logger;

		public TaskExecuter(ConnectionString connectionString, ILogProvider logProvider)
		{
			_connectionString = connectionString;
			_logProvider = logProvider;
			_logger = logProvider.GetLogger("RhetosJobs");
			_logger.Info("TaskExecuter initalized");

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

			// var actionType = _domainObjectModel.GetType(actionName);
			// var actionRepository = _actionPlugins.GetPlugin(actionName);
			// var parameters = JsonConvert.DeserializeObject(actionParameters, actionType);
			// actionRepository.Execute(parameters);


			using (var scope = new ProcessContainer(logProvider:_logProvider).CreateTransactionScopeContainer(CustomizeScope))
			{
				var logger = scope.Resolve<ILogProvider>().GetLogger("RhetosJobs");
				logger.Info("Inner process action execution started");
				var actions = scope.Resolve<INamedPlugins<IActionRepository>>();
				var actionType = scope.Resolve<IDomainObjectModel>().GetType(actionName);
				var actionRepository = actions.GetPlugin(actionName);
				var parameters = JsonConvert.DeserializeObject(actionParameters, actionType);
				actionRepository.Execute(parameters);
			
				scope.CommitChanges();
			}

			_logger.Info("ExecuteTask task executed");

		}

		private void CustomizeScope(ContainerBuilder builder)
		{
			builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
		}
	}
}