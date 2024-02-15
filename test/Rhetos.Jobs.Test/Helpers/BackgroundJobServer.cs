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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Jobs.Hangfire;

namespace Rhetos.Jobs.Test
{
    [TestClass]
    public static class BackgroundJobServer
    {
        ///// <summary>
        ///// In a standard application, Hangfire job server is started in Startup.Configure methods by calling app.UseRhetosHangfireServer().
        ///// In unit tests we need to manually initialize the RhetosJobsService and start the server.
        ///// </summary>
        [AssemblyInitialize]
        public static void HangfireJobsServiceInitialize(TestContext _)
        {
            RhetosJobsHangfireStartupExtensions.UseRhetosHangfireServer(TestScope.RhetosHost
                , ... new BackgroundJobServerOptions
                    {
                        ServerName =  Environment.MachineName + " unit tests",
                        Queues = new[] { "default", BackgroundJobServer.TestQueue1Name },
                        SchedulePollingInterval = TimeSpan.FromSeconds(1),
                    }
                );
        }

        internal const string TestQueue1Name = "test-queue-1";

        /// <summary>
        /// After unit tests are completed, any background jobs that are still running will be terminated, and ThreadAbortException might occur.
        /// These unit tests use HangfireAspNet infrastructure for background job, so there is an issue with clean termination:
        /// 1. Unit tests are not run as ASP.NET application, so Hangfire cannot detect shutdown (log contains: Hangfire.AspNet.ShutdownDetector|Shutdown detection setup failed).
        /// 2. BackgroundJobServer is not instanced manually, so we cannot call Dispose().
        /// 
        /// Waiting for jobs to complete is a partial solution to this issue, and it also helps to make sure that the created jobs are runnable.
        /// </summary>
        [AssemblyCleanup]
        public static void ShutdownHangfire()
        {
            RhetosHangfireHelper.WaitForJobsToComplete(null);
            TestScope.RhetosHost.GetRootContainer().Resolve<JobServersCollection>().ShutdownJobServers();
        }
    }
}
