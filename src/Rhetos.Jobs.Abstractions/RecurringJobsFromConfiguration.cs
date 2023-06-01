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

using Autofac;
using Rhetos.Dom;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Jobs
{

    /// <summary>
    /// Recurring jobs can be specified in configuration settings.
    /// This class schedules the recurring background job from configuration,
    /// and cancels the old scheduled jobs when removed from configuration.
    /// </summary>
    public class RecurringJobsFromConfiguration
    {
        private readonly ILogger _logger;
        private readonly RecurringJobsOptions _recurringJobsOptions;
        private readonly IBackgroundJobs _backgroundJobs;
        private readonly IDomainObjectModel _domainObjectModel;
        private readonly ISqlExecuter _sqlExecuter;

        /// <summary>
        /// Jobs created by this class have a specific suffix, so that this class can remove them when needed
        /// without affecting other manually created jobs.
        /// </summary>
        public static readonly string JobNameSuffix = "-from-configuration";

        public RecurringJobsFromConfiguration(
            ILogProvider logProvider, RecurringJobsOptions recurringJobsOptions, IBackgroundJobs backgroundJobs,
            IDomainObjectModel domainObjectModel, ISqlExecuter sqlExecuter)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _recurringJobsOptions = recurringJobsOptions;
            _backgroundJobs = backgroundJobs;
            _domainObjectModel = domainObjectModel;
            _sqlExecuter = sqlExecuter;
        }

        public void UpdateJobs()
        {
            CreteCustomLock(GetType().FullName);

            var oldJobs = new HashSet<string>(_backgroundJobs
                .ListRecurringJobs()
                .Where(name => name.EndsWith(JobNameSuffix, StringComparison.Ordinal)));

            var newJobs = _recurringJobsOptions.Recurring?.ToDictionary(job => job.Key + JobNameSuffix, job => job.Value)
                ?? new Dictionary<string, RecurringJobsDescription>();

            foreach (string jobName in oldJobs.Except(newJobs.Keys))
            {
                _logger.Info(() => $"Removing recurring job '{jobName}'.");
                _backgroundJobs.RemoveRecurringJob(jobName);
            }

            foreach (var job in newJobs)
            {
                string jobName = job.Key;
                if (!oldJobs.Contains(jobName))
                    _logger.Info(() => $"Adding a recurring job '{jobName}'.");
                else
                    _logger.Trace(() => $"Updating a recurring job '{jobName}'.");

                Type actionType = _domainObjectModel.GetType(job.Value.Action);
                if (actionType == null)
                    throw new ArgumentException($"Invalid configuration settings in {ConfigurationProvider.GetKey<RecurringJobsOptions>(o => o.Recurring)}:" +
                        $" There is no DSL Action with full name '{job.Value.Action}' in this application.");

                _backgroundJobs.SetRecurringJob<ActionJobExecuter, ActionJobParameter>(
                    jobName, job.Value.CronExpression, ActionJobParameter.FromActionName(job.Value.Action), job.Value.Queue, job.Value.RunAs);
            }
        }

        /// <summary>
        /// Manual database locking is used here in order to avoid deadlocks on parallel applications startup
        /// (may depend on usage of READ_COMMITTED_SNAPSHOT),
        /// </summary>
        private void CreteCustomLock(string key)
        {
            try
            {
                _sqlExecuter.ExecuteSql(
                    $@"DECLARE @lockResult int;
                    EXEC @lockResult = sp_getapplock {SqlUtility.QuoteText(key)}, 'Exclusive';
                    IF @lockResult < 0
                    BEGIN
                        RAISERROR('{key} lock.', 16, 10);
                        ROLLBACK;
                        RETURN;
                    END");
            }
            catch (FrameworkException ex) when (ex.Message.TrimEnd().EndsWith($"{key} lock.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Cannot updated the recurring jobs, because the this data is locked by another process that is still running.",
                    ex);
            }
        }
    }
}
