﻿/*
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
using Hangfire;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;

namespace Rhetos.Jobs.Hangfire
{
	/// <summary>
	/// This class is a job that is executed by Hangfire.
	/// </summary>
	/// <typeparam name="TExecuter"></typeparam>
	/// <typeparam name="TParameter"></typeparam>
    internal class RhetosExecutionContext<TExecuter, TParameter>
		where TExecuter : IJobExecuter<TParameter>
	{
		private readonly ILogger _logger;
		private readonly RhetosJobHangfireOptions _options;
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;

        public RhetosExecutionContext(ILogProvider logProvider, RhetosJobHangfireOptions options, IUnitOfWorkFactory unitOfWorkFactory)
		{
			// Note: The constructor parameters are resolved from the root DI container (set in UseAutofacActivator call in RhetosJobsService class).
			_logger = logProvider.GetLogger(InternalExtensions.LoggerName);
			_options = options;
            _unitOfWorkFactory = unitOfWorkFactory;
        }

		/// <summary>
		/// Executes the job in a new unit of work (in a separate transaction and a separate Rhetos DI scope).
		/// </summary>
		public void ExecuteUnitOfWork(JobParameter<TParameter> job)
		{
			_logger.Trace(() => $"ExecuteJob started.|{job.GetLogInfo(typeof(TExecuter))}");
			
			try
			{
				using (var scope = _unitOfWorkFactory.CreateScope(builder => CustomizeScope(builder, job.ExecuteAsUser, job.ExecuteAsAnonymous)))
				{
					_logger.Trace(() => $"ExecuteJob TransactionScopeContainer initialized.|{job.GetLogInfo(typeof(TExecuter))}");

					var rhetosHangfireJobs = scope.Resolve<RhetosHangfireJobs>();

					// Checking if the operation that inserted the Hangfire job have completed successfully,
					// to ensure the atomicity of that operation.
					if (!rhetosHangfireJobs.JobConfirmationExists(job.Id))
					{
						_logger.Trace(() =>
							$"Job no longer exists in queue. Transaction in which was job created was rolled back. Terminating execution.|{job.GetLogInfo(typeof(TExecuter))}");

						// Removing the recurring job from Hangfire queue, since it was not created successfully.
						if (!string.IsNullOrEmpty(job.RecurringJobName))
							RecurringJob.RemoveIfExists(job.RecurringJobName);

						return;
					}

					var jobExecuter = scope.Resolve<TExecuter>();
					jobExecuter.Execute(job.Parameter);

					if (string.IsNullOrEmpty(job.RecurringJobName))
						rhetosHangfireJobs.DeleteJobConfirmation(job.Id);
					scope.CommitAndClose();
				}
			}
			catch (Exception exception)
			{
				_logger.Error($"ExecuteJob exception: {exception}");
				throw;
			}

			_logger.Trace(() => $"ExecuteJob completed.|{job.GetLogInfo(typeof(TExecuter))}");
		}

		private void CustomizeScope(ContainerBuilder builder, string userName = null, bool? executeAsAnonymous = null)
		{
			if (!string.IsNullOrWhiteSpace(userName))
				builder.RegisterInstance(new JobExecuterUserInfo(userName)).As<IUserInfo>();
			else if (executeAsAnonymous == true)
				builder.RegisterInstance(new JobExecuterUserInfo()).As<IUserInfo>();
			else if (!string.IsNullOrWhiteSpace(_options.ProcessUserName))
				builder.RegisterInstance(new JobExecuterUserInfo(_options.ProcessUserName)).As<IUserInfo>();
			else
				builder.RegisterType(typeof(ProcessUserInfo)).As<IUserInfo>();
		}

        private class JobExecuterUserInfo : IUserInfo
        {
            private readonly string _userName;

            /// <summary>
            /// For anonymous user, set <paramref name="userName"/> to <see langword="null"/>.
            /// </summary>
            public JobExecuterUserInfo(string userName = null)
            {
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    _userName = userName;
                    IsUserRecognized = true;
                    Workstation = "Async job";
                }
            }

            public bool IsUserRecognized { get; }

            public string UserName => IsUserRecognized ? _userName : throw new ClientException("This operation is not supported for anonymous user."); // Standard Rhetos UserName implementation with exception.
			
			public string Workstation { get; }

            public string Report() => ReportUserNameOrAnonymous() + "," + Workstation;

            private string ReportUserNameOrAnonymous() => IsUserRecognized ? UserName : "<anonymous>";
        }
    }
}