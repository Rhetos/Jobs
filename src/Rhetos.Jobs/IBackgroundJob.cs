namespace Rhetos.Jobs
{
	public interface IBackgroundJob
	{
		/// <summary>
		/// Enqueues action for asynchronous execution after the transaction is completed
		/// </summary>
		/// <param name="action">Action which should be executed</param>
		/// <param name="executeInUserContext">If true Action will be executed in context of the user which started the transaction in which Action was enqueud. Otherwise it will be executed in context of service account.</param>
		/// <param name="optimizeDuplicates">If true previous same Actions (same Action with same parameters) will be removed from queue.</param>
		void EnqueueAction(object action, bool executeInUserContext, bool optimizeDuplicates);
	}
}