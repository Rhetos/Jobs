namespace Rhetos.Jobs.Hangfire
{
	public interface IJobExecuter
	{
		void ExecuteJob(IJob job);
	}
}