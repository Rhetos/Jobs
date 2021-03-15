namespace Rhetos.Jobs
{
	public interface IBackgroundJob
	{
		void Enqueue(object action, bool executeInUserContext = false, bool optimizeDuplicates = true);
	}
}