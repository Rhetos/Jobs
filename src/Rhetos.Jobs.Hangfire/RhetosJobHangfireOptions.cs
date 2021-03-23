namespace Rhetos.Jobs.Hangfire
{
	/// <summary>
	/// Rhetos.Jobs.Hangfire configuration settings
	/// </summary>
	[Options("Rhetos:Jobs:Hangfire")]
	public class RhetosJobHangfireOptions
	{
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
	}
}