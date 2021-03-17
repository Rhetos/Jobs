namespace Rhetos.Jobs.Hangfire
{
	/// <summary>
	/// Rhetos.Jobs.Hangfire configuration settings
	/// </summary>
	[Options("Rhetos:Jobs:Hangfire")]
	public class RhetosJobHangfireOptions
	{
		public RhetosJobHangfireOptions()
		{
			CommandBatchMaxTimeout = 300;
			SlidingInvisibilityTimeout = 300;
			QueuePollInterval = 0;
			UseRecommendedIsolationLevel = true;
			DisableGlobalLocks = true;
		}

		/// <summary>
		/// UserName under which enqueued actions will be executed if action is not enqueued with executeInUserContext=true. If ommited then UserName of the account of the app pool user will be used.
		/// </summary>
		public string ProcessUserName { get; set; }
		/// <summary>
		/// Value is in seconds. Default value is 500. For usage of the option see Hangfire documentation.
		/// </summary>
		public int CommandBatchMaxTimeout { get; set; }
		/// <summary>
		/// Value is in seconds. Default value is 500. For usage of the option see Hangfire documentation.
		/// </summary>
		public int SlidingInvisibilityTimeout { get; set; }
		/// <summary>
		/// Value is in seconds. Default value is 0. For usage of the option see Hangfire documentation.
		/// </summary>
		public int QueuePollInterval { get; set; }
		/// <summary>
		/// Default value is true. For usage of the option see Hangfire documentation.
		/// </summary>
		public bool UseRecommendedIsolationLevel { get; set; }
		/// <summary>
		/// Default value is true. For usage of the option see Hangfire documentation.
		/// </summary>
		public bool DisableGlobalLocks { get; set; }
	}
}