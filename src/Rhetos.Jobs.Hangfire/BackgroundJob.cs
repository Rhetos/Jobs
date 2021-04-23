using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Hangfire;
using Hangfire.States;

namespace Rhetos.Jobs.Hangfire
{
    public class BackgroundJob : IBackgroundJob
	{
		private readonly ISqlExecuter _sqlExecuter;
		private readonly IUserInfo _userInfo;
        private readonly RhetosHangfireInitialization _hangfireInitialization;
        private readonly ILogger _logger;
		private readonly ILogger _performanceLogger;

		private readonly List<JobSchedule> _jobInstances = new List<JobSchedule>();

		public BackgroundJob(
			ILogProvider logProvider,
			IPersistenceTransaction persistenceTransaction,
			ISqlExecuter sqlExecuter,
			IUserInfo userInfo,
			RhetosHangfireInitialization hangfireInitialization)
		{
			_sqlExecuter = sqlExecuter;
			_userInfo = userInfo;
            _hangfireInitialization = hangfireInitialization;
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

		private void EnqueueToHangfire(JobSchedule job)
		{
			_logger.Trace(()=> $"Enqueuing job in Hangfire.|{job.GetLogInfo()}");

			var commmand = $@"INSERT INTO Common.HangfireJob (ID) VALUES('{job.Job.Id}')";

			_sqlExecuter.ExecuteSql(commmand);

			job.EnqueueJob.Invoke();
			_logger.Trace(() => $"Job enqueued in Hangfire.|{job.GetLogInfo()}");
		}

        public void AddJob<TExecuter, TParameter>(TParameter parameter, bool executeInUserContext, object aggregationGroup = null, JobAggregator<TParameter> jobAggregator = null, string queue = null)
			where TExecuter : IJobExecuter<TParameter>
        {
			_hangfireInitialization.InitializeGlobalConfiguration();

			var newJob = new Job<TParameter>
			{
				Id = Guid.NewGuid(),
				ExecuteAsUser = executeInUserContext ? _userInfo.UserName : null,
				Parameter = parameter, // Might be updated later.
			};

			var schedule = new JobSchedule
			{
				Job = newJob,
				ExecuterType = typeof(TExecuter),
				ParameterType = typeof(TParameter),
				AggregationGroup = aggregationGroup,
				EnqueueJob = null, // Will be set later.
			};

			_logger.Trace(() => $"Enqueuing job.|{schedule.GetLogInfo()}");

			if (aggregationGroup != null)
			{
				var lastJobIndex = _jobInstances.FindLastIndex(oldJob =>
					schedule.ExecuterType == oldJob.ExecuterType
					&& schedule.ParameterType == oldJob.ParameterType
					&& schedule.Job.ExecuteAsUser == oldJob.Job.ExecuteAsUser
					&& schedule.AggregationGroup.Equals(oldJob.AggregationGroup));

				if (lastJobIndex >= 0)
				{
					if (jobAggregator == null)
						jobAggregator = DefaultAggregator;

					var oldJob = (Job<TParameter>)_jobInstances[lastJobIndex].Job;
					bool removeOld = jobAggregator(oldJob.Parameter, ref parameter);
					newJob.Parameter = parameter;

					if (removeOld)
					{
						_logger.Trace(() => $"Previous instance of the same job removed from queue." +
							$"|New {schedule.GetLogInfo()}|Old {_jobInstances[lastJobIndex].GetLogInfo()}");
						_jobInstances.RemoveAt(lastJobIndex);
					}
				}
			}

			if (string.IsNullOrWhiteSpace(queue) || queue.ToLower() == "default")
			{
				// Not enqueuing immediately to Hangfire, to allow later duplicate jobs to suppress the current one.
				schedule.EnqueueJob = () => global::Hangfire.BackgroundJob.Enqueue<JobExecuter<TExecuter, TParameter>>(
					executer => executer.ExecuteUnitOfWork(newJob));
			}
			else
			{
				// Not enqueuing immediately to Hangfire, to allow later duplicate jobs to suppress the current one.
				// Only way to use specific queue is to use new instance of BackgroundJobClient and set specific EnqueuedState.
				schedule.EnqueueJob = () =>
				{
					var client = new BackgroundJobClient();
					var state = new EnqueuedState(queue.ToLower());
					client.Create<JobExecuter<TExecuter, TParameter>>(e => e.ExecuteUnitOfWork(newJob), state);
				};
			}

			_jobInstances.Add(schedule);
			_logger.Trace(() => $"Job enqueued.|{schedule.GetLogInfo()}");
		}

		/// <summary>
		/// By default, duplicate jobs in the same aggregation group are eliminated.
		/// </summary>
		private static bool DefaultAggregator<TParameter>(TParameter oldJob, ref TParameter newJob) => true;
	}
}