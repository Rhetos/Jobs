namespace Rhetos.Jobs
{
	public interface IBackgroundJob
	{
		void Enqueue(object action);
	}
}