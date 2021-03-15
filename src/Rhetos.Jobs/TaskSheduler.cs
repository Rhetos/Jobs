using System;
using Newtonsoft.Json;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;

namespace Rhetos.Jobs
{
	public interface ITask : IEntity
	{
		string Name { get; set; }
		string Parameters { get; set; }
	}

	public class TaskSheduler
	{
		private readonly GenericRepository<ITask> _taskRepository;
		private readonly IDomainObjectModel _domainObjectModel;
		private readonly ILogger _logger;
		public TaskSheduler(ILogProvider logProvider, GenericRepository<ITask> taskRepository, IDomainObjectModel domainObjectModel)
		{
			_taskRepository = taskRepository;
			_domainObjectModel = domainObjectModel;
			_logger = logProvider.GetLogger("RhetosJobs");
		}

		// public void ScheduleTask(Guid taskId, int? waitInterval = null)
		// {
		// 	_logger.Info($"ScheduleTask|{taskId}|Begin");
		// 	Hangfire.BackgroundJob.Enqueue<TaskExecuter>(executer => executer.ExecuteTask(taskId));
		// 	_logger.Info($"ScheduleTask|{taskId}|Complete");
		//
		// 	if (waitInterval != null)
		// 		Thread.Sleep(waitInterval.Value);
		// }

		class Task : ITask
		{
			public Guid ID { get; set; }
			public string Name { get; set; }
			public string Parameters { get; set; }
		}

		public void ScheduleTask(ITask task)
		{
			_taskRepository.Insert(task);
			_logger.Info($"ScheduleTask|{task.ID}|Begin");
			Hangfire.BackgroundJob.Enqueue<TaskExecuter>(executer => executer.ExecuteTask(task.ID, task.Name, task.Parameters));
		}
	}
}