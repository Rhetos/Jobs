using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;

namespace Rhetos.Jobs.Hangfire
{
	public class BackgroundJob : IBackgroundJob
	{
		private readonly GenericRepository<IQueuedJob> _taskRepository;
		private readonly IUserInfo _userInfo;
		private readonly ILogger _logger;

		private readonly List<IJob> _jobInstances = new List<IJob>();

		public BackgroundJob(ILogProvider logProvider, GenericRepository<IQueuedJob> taskRepository, IPersistenceTransaction persistenceTransaction, IUserInfo userInfo)
		{
			_taskRepository = taskRepository;
			_userInfo = userInfo;
			_logger = logProvider.GetLogger("RhetosJobs");
			persistenceTransaction.BeforeClose += PersistenceTransactionOnBeforeClose;
		}

		private void PersistenceTransactionOnBeforeClose()
		{
			foreach (var job in _jobInstances) 
				EnqueueToHangfire(job);
		}

		private void EnqueueToHangfire(IJob job)
		{
			var queuedJob = _taskRepository.CreateInstance();
			queuedJob.ID = Guid.NewGuid();
			job.Id = queuedJob.ID;
			_taskRepository.Insert(queuedJob);

			global::Hangfire.BackgroundJob.Enqueue<IJobExecuter>(executer => executer.ExecuteJob(job));
		}

		public void Enqueue(object action, bool executeInUserContext = false, bool optimizeDuplicates = true)
		{
			var jobInstance = new Job
			{
				ActionName = action.GetType().FullName,
				ActionParameters = JsonConvert.SerializeObject(action)
			};

			if (executeInUserContext)
				jobInstance.ExecuteAsUser = _userInfo.UserName;

			if (optimizeDuplicates)
			{
				var index = _jobInstances.IndexOf(jobInstance);
				if (index >= 0)
					_jobInstances.RemoveAt(index);
			}

			_jobInstances.Add(jobInstance);
		}
	}
}