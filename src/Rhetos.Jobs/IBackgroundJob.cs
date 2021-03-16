namespace Rhetos.Jobs
{
	public interface IBackgroundJob
	{
		void EnqueueAction(object action, bool executeInUserContext = true, bool optimizeDuplicates = true);
	}
}