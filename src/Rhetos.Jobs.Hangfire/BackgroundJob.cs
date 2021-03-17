using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;

namespace Rhetos.Jobs.Hangfire
{
	public class BackgroundJob : IBackgroundJob
	{
		private readonly ISqlExecuter _sqlExecuter;
		private readonly IUserInfo _userInfo;
		private readonly ILogger _logger;
		private readonly ILogger _performanceLogger;

		private readonly List<Job> _jobInstances = new List<Job>();

		public BackgroundJob(ILogProvider logProvider, IPersistenceTransaction persistenceTransaction, ISqlExecuter sqlExecuter, IUserInfo userInfo)
		{
			_sqlExecuter = sqlExecuter;
			_userInfo = userInfo;
			_logger = logProvider.GetLogger(InternalExtensions.LoggerName);
			_performanceLogger = logProvider.GetLogger($"Performance.{InternalExtensions.LoggerName}");
			persistenceTransaction.BeforeClose += PersistenceTransactionOnBeforeClose;
		}

		private void PersistenceTransactionOnBeforeClose()
		{
			var stopWatch = Stopwatch.StartNew();

			foreach (var job in _jobInstances) 
				EnqueueToHangfire(job);

			_performanceLogger.Write(stopWatch, "Enqueue all jobs to Hangfire.");
		}

		private void EnqueueToHangfire(Job job)
		{
			_logger.Trace(()=> $"Enqueuing job in Hangfire.|{job.GetLogInfo()}");

			var commmand = $@"INSERT INTO RhetosJobs.HangfireJob (ID) VALUES('{job.Id}')";

			_sqlExecuter.ExecuteSql(commmand);

			global::Hangfire.BackgroundJob.Enqueue<IJobExecuter>(executer => executer.ExecuteJob(job));
			_logger.Trace(() => $"Job enqueued in Hangfire.|{job.GetLogInfo()}");
		}

		public void EnqueueAction(object action, bool executeInUserContext, bool optimizeDuplicates)
		{
			var job = new Job
			{
				Id = Guid.NewGuid(),
				ActionName = action.GetType().FullName,
				ActionParameters = JsonConvert.SerializeObject(action)
			};

			if (executeInUserContext)
				job.ExecuteAsUser = _userInfo.UserName;

			_logger.Trace(() => $"Enqueuing job.|{job.GetLogInfo()}");

			if (optimizeDuplicates)
			{
				var index = _jobInstances.IndexOf(job);
				while (index >= 0)
				{
					_jobInstances.RemoveAt(index);
					_logger.Trace(() => $"Previous instance of the same job removed from queue.|{job.GetLogInfo()}");
					index = _jobInstances.IndexOf(job);
				}
			}

			_jobInstances.Add(job);
			_logger.Trace(() => $"Job enqueued.|{job.GetLogInfo()}");
		}
	}
}