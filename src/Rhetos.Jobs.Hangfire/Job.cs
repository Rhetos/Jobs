using System;

namespace Rhetos.Jobs.Hangfire
{
	public interface IJob
	{
		Guid Id { get; set; }
		string ActionName { get; set; }
		string ActionParameters { get; set; }
		string ExecuteAsUser { get; set; }
	}

	public struct Job : IJob
	{
		public Guid Id { get; set; }
		public string ActionName { get; set; }
		public string ActionParameters { get; set; }
		public string ExecuteAsUser { get; set; }
	}
}