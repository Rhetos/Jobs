using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rhetos.Jobs.Hangfire
{
    public class BackgroundJob : IBackgroundJob
	{
		private readonly ISqlExecuter _sqlExecuter;
		private readonly IUserInfo _userInfo;
		private readonly ILogger _logger;
		private readonly ILogger _performanceLogger;

		private readonly List<JobScheduling> _jobInstances = new List<JobScheduling>();

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

		private void EnqueueToHangfire(JobScheduling job)
		{
			_logger.Trace(()=> $"Enqueuing job in Hangfire.|{job.GetLogInfo()}");

			var commmand = $@"INSERT INTO RhetosJobs.HangfireJob (ID) VALUES('{job.Job.Id}')";

			_sqlExecuter.ExecuteSql(commmand);

			job.EnqueueJob.Invoke();
			_logger.Trace(() => $"Job enqueued in Hangfire.|{job.GetLogInfo()}");
		}

        public void AddJob<TExecuter, TParameter>(TParameter parameter, bool executeInUserContext, object aggregationGroup, JobAggregator<TParameter> jobAggregator)
			where TExecuter : IJobExecuter<TParameter>
        {
			var job = new JobScheduling
			{
				Job = new Job
				{
					Id = Guid.NewGuid(),
					ExecuteAsUser = executeInUserContext ? _userInfo.UserName : null,
					Parameter = parameter, // Might be updated later.
				},
				ExecuterType = typeof(TExecuter),
				ParameterType = typeof(TParameter),
				AggregationGroup = aggregationGroup,
				EnqueueJob = null, // Will be set later.
			};

			_logger.Trace(() => $"Enqueuing job.|{job.GetLogInfo()}");

			if (aggregationGroup != null)
			{
				var lastJobIndex = _jobInstances.FindLastIndex(oldJob =>
					job.ExecuterType == oldJob.ExecuterType
					&& job.ParameterType == oldJob.ParameterType
					&& job.Job.ExecuteAsUser == oldJob.Job.ExecuteAsUser
					&& job.AggregationGroup.Equals(oldJob.AggregationGroup));

				if (lastJobIndex >= 0)
				{
					if (jobAggregator == null)
						jobAggregator = DefaultAggregator;

					bool removeOld = jobAggregator((TParameter)_jobInstances[lastJobIndex].Job.Parameter, ref parameter);
					job.Job.Parameter = parameter;

					if (removeOld)
					{
						_logger.Trace(() => $"Previous instance of the same job removed from queue." +
							$"|New {job.GetLogInfo()}|Old {_jobInstances[lastJobIndex].GetLogInfo()}");
						_jobInstances.RemoveAt(lastJobIndex);
					}
				}
			}

			// Not enqueuing immediately to Hangfire, to allow later duplicate jobs to suppress the current one.
			job.EnqueueJob = () => global::Hangfire.BackgroundJob.Enqueue<JobExecuter<TExecuter, TParameter>>(
				executer => executer.ExecuteUnitOfWork(job.Job));

			_jobInstances.Add(job);
			_logger.Trace(() => $"Job enqueued.|{job.GetLogInfo()}");
		}

		/// <summary>
		/// By default, duplicate jobs in the same aggregation group are eliminated.
		/// </summary>
#pragma warning disable IDE0060 // Remove unused parameter.
		private static bool DefaultAggregator<TParameter>(TParameter oldJob, ref TParameter newJob) => true;
#pragma warning restore IDE0060
	}
}