using System;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// Extended information on job, for job management before it is executed.
    /// </summary>
    public class JobSchedule
	{
		public Job Job { get; set; }
		public Type ExecuterType { get; set; }
		public Type ParameterType { get; set; }
		public object AggregationGroup { get; set; }
		public Action EnqueueJob { get; set; }

		public string GetLogInfo() => Job.GetLogInfo(ExecuterType);
	}
}