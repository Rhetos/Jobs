namespace Rhetos.Jobs
{
	public interface IBackgroundJob
	{
		void Enqueue(object action, bool executeInUserContext = true, bool optimizeDuplicates = true);
	}
}