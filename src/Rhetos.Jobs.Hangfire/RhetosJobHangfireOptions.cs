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

using Hangfire;
using Hangfire.SqlServer;
using System.Diagnostics.CodeAnalysis;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// Rhetos.Jobs.Hangfire configuration settings.
    /// It includes settings from <see cref="BackgroundJobServerOptions"/> and <see cref="SqlServerStorageOptions"/>.
    /// </summary>
    [Options("Rhetos:Jobs:Hangfire")]
	[SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Default options values are emphasized by setting them explicitly.")]
	public class RhetosJobHangfireOptions
	{
		/// <summary>
		/// If true Hangfire server will be initialized in Rhetos web application. Default value is true.
		/// </summary>
		/// <remarks>
		/// This options can be used to disable the Hangfire server initialization from a custom utility
		/// that uses Rhetos app's context, but does not want to run the background jobs.
		/// </remarks>
		public bool InitializeHangfireServer { get; set; } = true;

		/// <summary>
		/// UserName under which enqueued actions will be executed if action is not enqueued with executeInUserContext=true. If omitted then UserName of the account of the app pool user will be used.
		/// </summary>
		public string ProcessUserName { get; set; }

		/// <summary>
		/// Adds " Hangfire" suffix to the application name configured in connection string.
		/// </summary>
		public bool SetConnectionStringApplicationName { get; set; } = true;

		#region Options from Hangfire.SqlServer.SqlServerStorageOptions

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

		#endregion

        #region Options from Hangfire.BackgroundJobServerOptions

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
#pragma warning disable CA1819 // Properties should not return arrays
        public string[] Queues { get; set; } = { "default" };
#pragma warning restore CA1819 // Properties should not return arrays

        #endregion

		/// <summary>
		/// Default value is 10. Added to GlobalJobFilters.Filters via AutomaticRetryAttribute. For usage of the AutomaticRetryAttribute see Hangfire documentation.
		/// </summary>
		public int AutomaticRetryAttempts { get; set; } = 10;

		/// <summary>
		/// Delays in seconds for retrying failed jobs.
		/// For example, use "1, 60, 3600" to set the 1st retry after 1 second, 2nd after 1 minute, 3rd after 1 hour.
		/// The default value is Hangfire default (see the Hangfire documentation).
		/// </summary>
		public string DelaysInSeconds { get; set; }
    }
}