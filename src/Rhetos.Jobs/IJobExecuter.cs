using System;

namespace Rhetos.Jobs
{
	public interface IJobExecuter
	{
		void ExecuteJob(IJob job);
	}
}