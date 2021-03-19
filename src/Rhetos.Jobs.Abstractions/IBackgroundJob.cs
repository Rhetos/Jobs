namespace Rhetos.Jobs
{
	/// <summary>
	/// Background job execution service API.
	/// </summary>
	public interface IBackgroundJob
	{
		/// <summary>
		/// Enqueues action for asynchronous execution after the transaction is completed
		/// </summary>
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
		void EnqueueAction(object action, bool executeInUserContext, bool optimizeDuplicates);
	}
}