using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.Persistence;

namespace Rhetos.Jobs
{
	public class BackgroundJob : IBackgroundJob
	{
		private readonly GenericRepository<IJob> _taskRepository;
		private readonly ILogger _logger;

		private readonly List<JobInstance> _jobInstances = new List<JobInstance>();

		private struct JobInstance
		{
			public string ActionName { get; set; }
			public string ActionParameters { get; set; }
		}

		public BackgroundJob(ILogProvider logProvider, GenericRepository<IJob> taskRepository, IPersistenceTransaction persistenceTransaction)
		{
			_taskRepository = taskRepository;
			_logger = logProvider.GetLogger("RhetosJobs");
			persistenceTransaction.BeforeClose += PersistenceTransactionOnBeforeClose;
		}

		private void PersistenceTransactionOnBeforeClose()
		{
			foreach (var jobInstance in _jobInstances) 
				EnqueueToHangfire(jobInstance);
		}

		private void EnqueueToHangfire(JobInstance jobInstance)
		{
			var job = _taskRepository.CreateInstance();
			job.ID = Guid.NewGuid();
			_taskRepository.Insert(job);

			Hangfire.BackgroundJob.Enqueue<IJobExecuter>(executer => executer.ExecuteJob(job.ID, jobInstance.ActionName, jobInstance.ActionParameters));
		}

		public void Enqueue(object action)
		{
			var jobInstance = new JobInstance
			{
				ActionName = action.GetType().FullName,
				ActionParameters = JsonConvert.SerializeObject(action)
			};

			var index = _jobInstances.IndexOf(jobInstance);
			if (index >= 0)
				_jobInstances.RemoveAt(index);

			_jobInstances.Add(jobInstance);
		}
	}
}