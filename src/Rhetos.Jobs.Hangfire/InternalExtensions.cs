using System;

namespace Rhetos.Jobs.Hangfire
{
	internal static class InternalExtensions
	{
		public const string LoggerName = "RhetosJobs";
		public static string LogInfo(this IJob job)
		{
			var jobId = job.Id == Guid.Empty ? "" : $"Jobid: {job.Id}|";
			var userInfo = string.IsNullOrWhiteSpace(job.ExecuteAsUser) ? "" : $"ExecuteInUserContext: {job.ExecuteAsUser}|";
			return $"{jobId}{userInfo}Action: {job.ActionName}|Parameters: {job.ActionParameters}";
		}
	}
}