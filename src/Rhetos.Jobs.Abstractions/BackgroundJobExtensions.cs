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

using Newtonsoft.Json;
using Rhetos.Jobs;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class BackgroundJobExtensions
    {
        /// <summary>
        /// Enqueues a DSL Action for asynchronous execution after the transaction is completed
        /// </summary>
        /// <param name="backgroundJob"></param>
        /// <param name="action">
        /// Action which should be executed.
        /// It is instance of the action type, that contains action parameters.
        /// </param>
        /// <param name="executeInUserContext">
        /// If true, Action will be executed in context of the user which started the transaction in which Action was enqueued.
        /// Otherwise it will be executed in context of service account.
        /// Note that the action execution will not automatically check for the user's claims.
        /// </param>
        /// <param name="optimizeDuplicates">
        /// If true, previous same Actions (same Action with same parameters) within the current scope (web request) will be removed from queue.
        /// </param>
        /// <param name="queue">
        /// Name of the queue. Default is null.
        /// </param>
        public static void EnqueueAction(this IBackgroundJobs backgroundJob, object action, bool executeInUserContext, bool optimizeDuplicates, string queue = null)
        {
            var jobParameters = ActionJobParameter.FromActionInstance(action);

            backgroundJob.AddJob<ActionJobExecuter, ActionJobParameter>(
                jobParameters,
                executeInUserContext,
                optimizeDuplicates ? JsonConvert.SerializeObject(jobParameters) : null,
                null,
                queue);
        }

        /// <summary>
        /// Schedules a recurring background job that executed the provided DSL Action.
        /// If a recurring job with the same <paramref name="jobName"/> already exists, it will be updated.
        /// The job will not be scheduled if the current scope (web request) fails.
        /// </summary>
        /// <param name="backgroundJobs"></param>
        /// <param name="jobName">
        /// Unique job name. If a job with the same name already exists, it will be overwritten.
        /// </param>
        /// <param name="action">
        /// Action which should be executed.
        /// It is instance of the action type, that contains action parameters.
        /// </param>
        /// <param name="cronExpression">
        /// A pattern that describes the job schedule: when and how often the job is executed.
        /// See <see href="https://en.wikipedia.org/wiki/Cron#CRON_expression"/> for basic information.
        /// </param>
        /// <param name="queue">
        /// Name of the queue. Default is null.
        /// </param>
        /// <param name="runAs">
		/// The job will run in a context of the specified Rhetos app user.
		/// The provided value should exist in the table Common.Principal, column Name.
		/// </param>
        public static void SetRecurringAction(this IBackgroundJobs backgroundJobs, string jobName, object action, string cronExpression, string queue = null, string runAs = null)
        {
            var jobParameters = ActionJobParameter.FromActionInstance(action);

            backgroundJobs.SetRecurringJob<ActionJobExecuter, ActionJobParameter>(
                jobName, cronExpression, jobParameters, queue, runAs);
        }
    }
}
