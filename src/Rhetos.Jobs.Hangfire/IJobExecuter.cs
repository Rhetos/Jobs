namespace Rhetos.Jobs.Hangfire
{
	public interface IJobExecuter
	{
		void ExecuteJob(Job job);
	}
}