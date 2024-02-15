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
	/// <summary>
	/// Implementation of IBackgroundJobs that uses Hangfire to schedule the jobs.
	/// </summary>
	public class BackgroundJobs : IBackgroundJobs
	{
		private readonly IUserInfo _userInfo;
        private readonly RhetosHangfireInitialization _hangfireInitialization;
        private readonly RhetosHangfireJobs _rhetosHangfireJobs;
        private readonly ILogger _logger;
		private readonly ILogger _performanceLogger;

		private readonly List<JobSchedule> _jobInstances = new();
        private const string HangfireDefaultQueueName = "default";

		public BackgroundJobs(
			ILogProvider logProvider,
			IPersistenceTransaction persistenceTransaction,
			IUserInfo userInfo,
			RhetosHangfireInitialization hangfireInitialization,
			RhetosHangfireJobs rhetosHangfireJobs)
		{
			_userInfo = userInfo;
            _hangfireInitialization = hangfireInitialization;
            _rhetosHangfireJobs = rhetosHangfireJobs;
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

            _rhetosHangfireJobs.InsertJobConfirmation(job.Job.Id, job.Job.RecurringJobName);

			job.EnqueueJob.Invoke();
			_logger.Trace(() => $"Job enqueued in Hangfire.|{job.GetLogInfo()}");
		}

        /// <summary>
        /// Hangfire's convention is to use lowercase for queue names.
		/// If queue name is not specified, the default queue is used.
        /// </summary>
        private static string NormalizeQueueName(string queue) => queue?.ToLowerInvariant() ?? HangfireDefaultQueueName;

        public void AddJob<TExecuter, TParameter>(TParameter parameter, bool executeInUserContext, object aggregationGroup = null, JobAggregator<TParameter> jobAggregator = null, string queue = null)
			where TExecuter : IJobExecuter<TParameter>
        {
			_hangfireInitialization.InitializeGlobalConfiguration();
			queue = NormalizeQueueName(queue);

			var newJob = new JobParameter<TParameter>
			{
				Id = Guid.NewGuid(),
				RecurringJobName = null,
				ExecuteAsUser = executeInUserContext && _userInfo.IsUserRecognized ? _userInfo.UserName : null,
				ExecuteAsAnonymous = executeInUserContext && !_userInfo.IsUserRecognized ? true : null, // The null value is considered false, to simplify job parameter serialization.
				Parameter = parameter, // Might be updated later when applying jobAggregator.
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
					&& schedule.Job.ExecuteAsAnonymous == oldJob.Job.ExecuteAsAnonymous
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

				// Not enqueuing immediately to Hangfire, to allow later duplicate jobs to suppress the current one.
			schedule.EnqueueJob = () => BackgroundJob.Enqueue<RhetosExecutionContext<TExecuter, TParameter>>(
				context => context.ExecuteUnitOfWork(newJob, queue));

			_jobInstances.Add(schedule);
			_logger.Trace(() => $"Job created.|{schedule.GetLogInfo()}");
		}

		/// <summary>
		/// By default, duplicate jobs in the same aggregation group are eliminated.
		/// </summary>
		private static bool DefaultAggregator<TParameter>(TParameter oldJob, ref TParameter newJob) => true;

		public void SetRecurringJob<TExecuter, TParameter>(string name, string cronExpression, TParameter parameter, string queue = null, string runAs = null)
			where TExecuter : IJobExecuter<TParameter>
		{
			_hangfireInitialization.InitializeGlobalConfiguration();
			queue = NormalizeQueueName(queue);

			// The name is required for recurring jobs in order to:
			// 1. recognize a duplicate job initialization (for example on app startup),
			// 2. to remove the job when no longer needed.
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("Recurring job must have a name.", nameof(name));

			var newJob = new JobParameter<TParameter>
			{
				Id = _rhetosHangfireJobs.GetJobId(name) ?? Guid.NewGuid(),
				RecurringJobName = name,
				ExecuteAsUser = runAs,
				ExecuteAsAnonymous = null,
				Parameter = parameter,
			};

			var schedule = new JobSchedule
			{
				Job = newJob,
				ExecuterType = typeof(TExecuter),
				ParameterType = typeof(TParameter),
				AggregationGroup = null,
				EnqueueJob = () => RecurringJob.AddOrUpdate<RhetosExecutionContext<TExecuter, TParameter>>(
					name, context => context.ExecuteUnitOfWork(newJob, queue), cronExpression, TimeZoneInfo.Local, queue)
			};

			_jobInstances.Add(schedule);
			_logger.Trace(() => $"Recurring job created.|{name}|{schedule.GetLogInfo()}");
	}

		public IEnumerable<string> ListRecurringJobs()
		{
			return _rhetosHangfireJobs.GetJobNames();
		}

		public void RemoveRecurringJob(string name)
        {
			_hangfireInitialization.InitializeGlobalConfiguration();

			if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Recurring job must have a name.", nameof(name));

            Guid? id = _rhetosHangfireJobs.GetJobId(name);

            if (id != null)
				_rhetosHangfireJobs.DeleteJobConfirmation(id.Value);
            else
                _logger.Trace($"Missing job confirmation entry for '{name}'.");
            RecurringJob.RemoveIfExists(name);
        }
    }
}