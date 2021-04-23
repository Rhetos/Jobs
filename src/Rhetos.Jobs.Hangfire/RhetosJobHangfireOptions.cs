using System;

namespace Rhetos.Jobs.Hangfire
{
	/// <summary>
	/// Rhetos.Jobs.Hangfire configuration settings
	/// </summary>
	[Options("Rhetos:Jobs:Hangfire")]
	public class RhetosJobHangfireOptions
	{
		/// <summary>
		/// If true Hangfire server will be initialized in Rhetos web application. Default value is true.
		/// </summary>
		public bool InitializeHangfireServer { get; set; } = true;
		/// <summary>
		/// UserName under which enqueued actions will be executed if action is not enqueued with executeInUserContext=true. If ommited then UserName of the account of the app pool user will be used.
		/// </summary>
		public string ProcessUserName { get; set; }

		/// <summary>
		/// Value is in seconds. Default value is 300. For usage of the option see Hangfire documentation.
		/// </summary>
		public int CommandBatchMaxTimeout { get; set; } = 300;

		/// <summary>
		/// Value is in seconds. Default value is 300. For usage of the option see Hangfire documentation.
		/// </summary>
		public int SlidingInvisibilityTimeout { get; set; } = 300;

		/// <summary>
		/// Value is in seconds. Default value is 0. For usage of the option see Hangfire documentation.
		/// </summary>
		public int QueuePollInterval { get; set; } = 0;

		/// <summary>
		/// Default value is true. For usage of the option see Hangfire documentation.
		/// </summary>
		public bool UseRecommendedIsolationLevel { get; set; } = true;

		/// <summary>
		/// Default value is true. For usage of the option see Hangfire documentation.
		/// </summary>
		public bool DisableGlobalLocks { get; set; } = true;

		/// <summary>
		/// Default value for WorkerCount should be 1 or 2 for common Rhetos applications,
		/// to avoid overloading the database, since the processing bottleneck is usually in the database.
		/// For usage of the option see Hangfire documentation.
		/// </summary>
		public int WorkerCount { get; set; } = 2;
		/// <summary>
		/// Value is in seconds. Default value is 15. For usage of the option see Hangfire documentation.
		/// </summary>
		public int ShutdownTimeout { get; set; } = 15;
		/// <summary>
		/// Value is in seconds. Default value is 0. For usage of the option see Hangfire documentation.
		/// </summary>
		public int StopTimeout { get; set; } = 0;
		/// <summary>
		/// Value is in seconds. Default value is 15. For usage of the option see Hangfire documentation.
		/// </summary>
		public int SchedulePollingInterval { get; set; } = 15;
		/// <summary>
		/// Value is in seconds. Default value is 30. For usage of the option see Hangfire documentation.
		/// </summary>
		public int HeartbeatInterval { get; set; } = 30;
		/// <summary>
		/// Value is in seconds. Default value is 300. For usage of the option see Hangfire documentation.
		/// </summary>
		public int ServerTimeout { get; set; } = 300;
		/// <summary>
		/// Value is in seconds. Default value is 300. For usage of the option see Hangfire documentation.
		/// </summary>
		public int ServerCheckInterval { get; set; } = 300;
		/// <summary>
		/// Value is in seconds. Default value is 5. For usage of the option see Hangfire documentation.
		/// </summary>
		public int CancellationCheckInterval { get; set; } = 5;
		/// <summary>
		/// Array of queue names which will be processed by this instance of Hangfire server. Default is '["default"]'.
		/// </summary>
		public string[] Queues { get; set; } = {"default"};
	}
}