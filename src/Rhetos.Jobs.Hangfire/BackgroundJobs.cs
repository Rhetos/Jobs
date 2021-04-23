/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

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
    public class BackgroundJobs : IBackgroundJobs
	{
		private readonly ISqlExecuter _sqlExecuter;
		private readonly IUserInfo _userInfo;
        private readonly RhetosHangfireInitialization _hangfireInitialization;
        private readonly ILogger _logger;
		private readonly ILogger _performanceLogger;

		private readonly List<JobSchedule> _jobInstances = new List<JobSchedule>();

		public BackgroundJobs(
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

			var newJob = new JobParameter<TParameter>
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

					var oldJob = (JobParameter<TParameter>)_jobInstances[lastJobIndex].Job;
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
				schedule.EnqueueJob = () => global::Hangfire.BackgroundJob.Enqueue<RhetosExecutionContext<TExecuter, TParameter>>(
					context => context.ExecuteUnitOfWork(newJob));
			}
			else
			{
				// Not enqueuing immediately to Hangfire, to allow later duplicate jobs to suppress the current one.
				// Only way to use specific queue is to use new instance of BackgroundJobClient and set specific EnqueuedState.
				schedule.EnqueueJob = () =>
				{
					var client = new BackgroundJobClient();
					var state = new EnqueuedState(queue.ToLower());
					client.Create<RhetosExecutionContext<TExecuter, TParameter>>(context => context.ExecuteUnitOfWork(newJob), state);
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