using System;

namespace Rhetos.Jobs
{
    /// <summary>
    /// Background job execution service API.
    /// </summary>
    public interface IBackgroundJob
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
