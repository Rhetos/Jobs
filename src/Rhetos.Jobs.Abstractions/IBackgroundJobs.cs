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

using System;
using System.Collections.Generic;

namespace Rhetos.Jobs
{
    /// <summary>
    /// Background job execution service API.
    /// </summary>
    public interface IBackgroundJobs
	{
		/// <summary>
		/// Creates a new background job that will be executed after the current transaction is completed.
		/// </summary>
		/// <typeparam name="TExecuter">
		/// Class that will execute the job. Must implement <see cref="IJobExecuter{TParameter}"/>.
		/// </typeparam>
		/// <typeparam name="TParameter">
		/// Job parameter that will be provided to job executer.
		/// </typeparam>
		/// <param name="parameter">
		/// Job parameters.
		/// </param>
		/// <param name="executeInUserContext">
		/// If true, the job will be executed in context of the user which started the transaction in which Action was enqueued.
		/// Otherwise it will be executed in context of service account.
		/// Note that the action execution will not automatically check for the user's claims.
		/// </param>
		/// <param name="aggregationGroup">
		/// Optional.
		/// Allows removing previous duplicate jobs within the current scope (web request).
		/// If set to null, aggregation is turned off and duplicate jobs will not be removed.
		/// The jobs of the same type, with the same aggregation group, will be sent to <paramref name="jobAggregator"/>.
		/// A default implementation of the aggregator is to simply removed the old job within the same aggregation group.
		/// The value can be any type (e.g. a string or anonymous type). If a custom class is used, it should override <see cref="object.Equals(object)"/> and <see cref="object.GetHashCode"/>.
		/// </param>
		/// <param name="jobAggregator">
		/// Optional.
		/// Allows removing previous duplicate jobs within the current scope (web request) and combining work of multiple jobs into one.
		/// If set to null, a default implementation will be applied, that simply removes the old job within the same <paramref name="aggregationGroup"/>.
		/// It will be called for each new job, with provided last instance of the same job type, user context and the same aggregation group, if such last instance exists.
		/// See <see cref="JobAggregator{TParameter}"/> for more details.
		/// </param>
		/// <param name="queue">
		/// Name of the queue. Default is null.
		/// </param>
		void AddJob<TExecuter, TParameter>(TParameter parameter, bool executeInUserContext, object aggregationGroup = null, JobAggregator<TParameter> jobAggregator = null, string queue = null)
			where TExecuter : IJobExecuter<TParameter>;

		/// <summary>
		/// Schedules a recurring background job.
		/// If a recurring job with the same <paramref name="name"/> already exists, it will be updated.
		/// The job will not be scheduled it the current scope (web request) fails.
		/// </summary>
		/// <typeparam name="TExecuter">
		/// Class that will execute the job. Must implement <see cref="IJobExecuter{TParameter}"/>.
		/// </typeparam>
		/// <typeparam name="TParameter">
		/// Job parameter that will be provided to job executer.
		/// </typeparam>
		/// <param name="name">
		/// Unique job name. If a job with the same name already exists, it will be overwritten.
		/// </param>
		/// <param name="cronExpression">
		/// A pattern that describes the job schedule: when and how often the job is executed.
		/// See <see href="https://en.wikipedia.org/wiki/Cron#CRON_expression"/> for basic information.
		/// </param>
		/// <param name="parameter">
		/// Job parameters.
		/// </param>
		/// <param name="queue">
		/// Name of the queue. Default is null.
		/// </param>
		void SetRecurringJob<TExecuter, TParameter>(string name, string cronExpression, TParameter parameter, string queue = null)
			where TExecuter : IJobExecuter<TParameter>;

		/// <summary>
		/// Returns a list of names of the recurring jobs that were created by <see cref="SetRecurringJob"/>.
		/// </summary>
		IEnumerable<string> ListRecurringJobs();

		/// <summary>
		/// Removed the jobs that was created by <see cref="SetRecurringJob"/>.
		/// </summary>
		/// <remarks>
		/// If the job does not exist, it the method will *not* throw an exception.
		/// </remarks>
		void RemoveRecurringJob(string name);
	}

	/// <summary>
	/// Removes duplicate jobs by aggregating the job parameters into a single one, if applicable.
	/// This is achieved by modifying the newJob to include work from oldJob (if applicable), and returning true to signal that the oldJob should be skipped.
	/// The aggregator delegate will be called for each new job, with provided last instance of the same job type, user context and the same aggregation group.
	/// The job aggregation is done only on jobs within a single unit of work (a single web request).
	/// </summary>
	/// <typeparam name="TParameter">Job parameter type.</typeparam>
	/// <param name="oldJob">Previous job parameter.</param>
	/// <param name="newJob">New job parameter. May be modified to cover the work from the oldJob.</param>
	/// <returns>Returns true if newJob covers all work that is specified in oldJob, therefore the oldJob should be skipped.</returns>
	public delegate bool JobAggregator<TParameter>(TParameter oldJob, ref TParameter newJob);
}
