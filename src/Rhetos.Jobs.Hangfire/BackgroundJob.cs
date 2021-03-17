using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private readonly ILogger _performanceLogger;

		private readonly List<Job> _jobInstances = new List<Job>();

		public BackgroundJob(ILogProvider logProvider, GenericRepository<IQueuedJob> taskRepository, IPersistenceTransaction persistenceTransaction, IUserInfo userInfo)
		{
			_taskRepository = taskRepository;
			_userInfo = userInfo;
			_logger = logProvider.GetLogger(InternalExtensions.LoggerName);
			_performanceLogger = logProvider.GetLogger($"Performance.{InternalExtensions.LoggerName}");
			persistenceTransaction.BeforeClose += PersistenceTransactionOnBeforeClose;
		}

		private void PersistenceTransactionOnBeforeClose()
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			foreach (var job in _jobInstances) 
				EnqueueToHangfire(job);

			_performanceLogger.Write(stopWatch, "Enqueue all jobs to Hangfire.");
		}

		private void EnqueueToHangfire(Job job)
		{
			job.Id = Guid.NewGuid();
			var jobInfo = job.GetLogInfo();
			_logger.Trace($"Enqueuing job in Hangfire.|{jobInfo}");

			var queuedJob = _taskRepository.CreateInstance();
			queuedJob.ID = job.Id;
			_taskRepository.Insert(queuedJob);

			global::Hangfire.BackgroundJob.Enqueue<IJobExecuter>(executer => executer.ExecuteJob(job));
			_logger.Trace($"Job enqueued in Hangfire.|{jobInfo}");
		}

		public void EnqueueAction(object action, bool executeInUserContext, bool optimizeDuplicates)
		{
			var job = new Job
			{
				ActionName = action.GetType().FullName,
				ActionParameters = JsonConvert.SerializeObject(action)
			};

			if (executeInUserContext)
				job.ExecuteAsUser = _userInfo.UserName;

			var jobInfo = job.GetLogInfo();
			_logger.Trace($"Enqueuing job.|{jobInfo}");

			if (optimizeDuplicates)
			{
				var index = _jobInstances.IndexOf(job);
				while (index >= 0)
				{
					_jobInstances.RemoveAt(index);
					_logger.Trace($"Previous instance of the same job removed from queue.|{jobInfo}");
					index = _jobInstances.IndexOf(job);
				}
			}

			_jobInstances.Add(job);
			_logger.Trace($"Job enqueued.|{jobInfo}");
		}
	}
}