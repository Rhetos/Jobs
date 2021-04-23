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

using Autofac;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;

namespace Rhetos.Jobs.Hangfire
{
    public class JobExecuter<TExecuter, TParameter>
		where TExecuter : IJobExecuter<TParameter>
	{
		private readonly ILogger _logger;
		private readonly RhetosJobHangfireOptions _options;

        public JobExecuter(ILogProvider logProvider, RhetosJobHangfireOptions options)
		{
			// Note: The constructor parameters are resolved from the root DI container (set in UseAutofacActivator call in RhetosJobsService class).
			_logger = logProvider.GetLogger(InternalExtensions.LoggerName);
			_options = options;
        }

		/// <summary>
		/// Executes the job in a new unit of work (in a separate transaction and a separate Rhetos DI scope).
		/// </summary>
		public void ExecuteUnitOfWork(Job<TParameter> job)
		{
			_logger.Trace(() => $"ExecuteJob started.|{job.GetLogInfo(typeof(TExecuter))}");
			
			try
			{
				using (var scope = RhetosJobServer.CreateScope(builder => CustomizeScope(builder, job.ExecuteAsUser)))
				{
					_logger.Trace(() => $"ExecuteJob TransactionScopeContainer initialized.|{job.GetLogInfo(typeof(TExecuter))}");

					var sqlExecuter = scope.Resolve<ISqlExecuter>();
					if (!JobExists(job.Id, sqlExecuter))
					{
						_logger.Trace(() =>
							$"Job no longer exists in queue. Transaction in which was job created was rolled back. Terminating execution.|{job.GetLogInfo(typeof(TExecuter))}");
						return;
					}

					var jobExecuter = scope.Resolve<TExecuter>();
					jobExecuter.Execute(job.Parameter);

					DeleteJob(job.Id, sqlExecuter);
					scope.CommitChanges();
				}
			}
			catch (Exception exception)
			{
				_logger.Error($"ExecuteJob exception: {exception}");
				throw;
			}

			_logger.Trace(() => $"ExecuteJob completed.|{job.GetLogInfo(typeof(TExecuter))}");
		}

		private static bool JobExists(Guid jobId, ISqlExecuter sqlExecuter)
		{
			var command = $"SELECT COUNT(1) FROM Common.HangfireJob WITH (READCOMMITTEDLOCK, ROWLOCK) WHERE ID = '{jobId}'";
			var count = 0;

			sqlExecuter.ExecuteReader(command, reader => count = reader.GetInt32(0));

			//If transaction that created job failed there will be no job scheduled and count will be zero
			return count > 0;
		}

		private static void DeleteJob(Guid jobId, ISqlExecuter sqlExecuter)
		{
			var command = $"DELETE FROM Common.HangfireJob WHERE ID = '{jobId}'";
			sqlExecuter.ExecuteSql(command);
		}

		private void CustomizeScope(ContainerBuilder builder, string userName = null)
		{
			if (!string.IsNullOrWhiteSpace(userName))
				builder.RegisterInstance(new JobExecuterUserInfo(userName)).As<IUserInfo>();
			else if (!string.IsNullOrWhiteSpace(_options.ProcessUserName))
				builder.RegisterInstance(new JobExecuterUserInfo(_options.ProcessUserName)).As<IUserInfo>();
			else
				builder.RegisterType(typeof(ProcessUserInfo)).As<IUserInfo>();
		}

		private class JobExecuterUserInfo : IUserInfo
		{
			public JobExecuterUserInfo(string userName)
			{
				UserName = userName;
				IsUserRecognized = true;
				Workstation = "Async job";
			}

			public bool IsUserRecognized { get; }
			public string UserName { get; }
			public string Workstation { get; }

			public string Report() { return UserName + "," + Workstation; }
		}
	}
}