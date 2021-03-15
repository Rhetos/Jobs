using System;

namespace Rhetos.Jobs
{
	public interface IJobExecuter
	{
		void ExecuteJob(Guid jobId, string actionName, string actionParameters);
	}
}