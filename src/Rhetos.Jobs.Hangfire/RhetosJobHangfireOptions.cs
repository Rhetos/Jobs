/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

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
		/// <summary>
		/// Default value is 10. Added to GlobalJobFilters.Filters via AutomaticRetryAttribute. For usage of the AutomaticRetryAttribute see Hangfire documentation.
		/// </summary>
		public int AutomaticRetryAttempts { get; set; } = 10;
		/// <summary>
		/// Delays in seconds for retry jobs (i.e. "1, 60, 3600"). Default value is Hangfire default. For usage of the default algorithm see Hangfire documentation.
		/// </summary>
		public string DelaysInSeconds { get; set; }
	}
}