using Autofac;
using Hangfire;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Jobs.Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Jobs.Test
{
    [TestClass]
    public static class HangfireJobsTestInfrastructure
    {
        /// <summary>
        /// In a standard application, all IService plugins are initialized on startup from RhetosRuntime.cs.
        /// In unit tests we need to manually initialize the RhetosJobsService.
        /// </summary>
        [AssemblyInitialize]
        public static void HangfireJobsServiceInitialize(TestContext _)
        {
            var containerField = typeof(ProcessContainer).GetField("_rhetosIocContainer", BindingFlags.NonPublic | BindingFlags.Instance);
            var container = (Lazy<IContainer>)containerField.GetValue(RhetosProcessHelper.ProcessContainer);

            RhetosJobServer.ConfigureHangfireJobServers(container.Value);
            BackgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions { ServerName =  Environment.MachineName + " unit tests" });
        }

        public static BackgroundJobServer BackgroundJobServer;

        public static IConfigurationBuilder SetHangfireTestConfiguration(this IConfigurationBuilder configBuilder)
        {
            // Setting SlidingInvisibilityTimeout to quickly evaluate any jobs that were not executed immediately after enqueuing.
            configBuilder.AddKeyValue("Rhetos:Jobs:Hangfire:SlidingInvisibilityTimeout", 1);
            return configBuilder;
        }

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
        }
    }
}
