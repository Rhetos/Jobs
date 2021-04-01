namespace Rhetos.Jobs
{
	/// <summary>
	/// Implement this interface to add a custom background job type.
	/// Existing implementation, <see cref="ActionJobExecuter"/>, may be used for jobs that are
	/// implemented simply as a DSL Action.
	/// </summary>
	/// <remarks>
	/// The implementation needs to be registered to Rhetos DI container.
	/// It will be resolved from DI container scope when the job is executed,
	/// as a unit of work (a separate database transaction).
	/// </remarks>
	/// <typeparam name="TParameter">Job parameter type.</typeparam>
	public interface IJobExecuter<in TParameter>
	{
		/// <summary>
		/// Executes the job immediately.
		/// </summary>
		void Execute(TParameter job);
	}
}