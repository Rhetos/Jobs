using System;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// Job parameters required for job execution.
    /// It is serialized to the job queue storage before executing it.
    /// </summary>
    public class Job
	{
		public Guid Id { get; set; }
		public object Parameter { get; set; }
		public string ExecuteAsUser { get; set; }

		public string GetLogInfo(Type executerType)
		{
			var userInfo = string.IsNullOrWhiteSpace(ExecuteAsUser) ? "User not specified" : $"ExecuteInUserContext: {ExecuteAsUser}";
			return $"JobId: {Id}|{userInfo}|{executerType}|{Parameter}";
		}
	}
}