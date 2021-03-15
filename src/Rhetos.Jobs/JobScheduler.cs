using System;
using Newtonsoft.Json;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;

namespace Rhetos.Jobs
{
	public interface IJobScheduler
	{
		void ScheduleJob(object action);
	}

	public class JobScheduler : IJobScheduler
	{
		private readonly GenericRepository<IJob> _taskRepository;
		private readonly ILogger _logger;
		public JobScheduler(ILogProvider logProvider, GenericRepository<IJob> taskRepository)
		{
			_taskRepository = taskRepository;
			_logger = logProvider.GetLogger("RhetosJobs");
		}

		public void ScheduleJob(object action)
		{
			var job = _taskRepository.CreateInstance();
			job.ID = Guid.NewGuid();
			_taskRepository.Insert(job);

			var actionName = action.GetType().FullName;
			var actionParameters = JsonConvert.SerializeObject(action);
			Hangfire.BackgroundJob.Enqueue<IJobExecuter>(executer => executer.ExecuteJob(job.ID, actionName, actionParameters));
		}
	}
}