using System;

namespace Rhetos.Jobs.Hangfire
{
	public struct Job
	{
		public Guid Id { get; set; }
		public string ActionName { get; set; }
		public string ActionParameters { get; set; }
		public string ExecuteAsUser { get; set; }
	}
}