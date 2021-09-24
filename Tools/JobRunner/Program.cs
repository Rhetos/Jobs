using Autofac;
using Hangfire;
using Rhetos;
using Rhetos.Jobs.Hangfire;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;

namespace JobRunner
{
    /// <summary>
    /// This is a demo CLI utility for running background job separately for the Rhetos web app.
    /// </summary>
    static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Missing command-line argument: path to the Rhetos app assembly");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("JobRunner.exe <path to the Rhetos app assembly>");
                return 1;
            }
            // First command=line argument is a relative path to the Rhetos application's assembly, for example "Bookstore.dll"
            string rhetosAppPath = args[0];
            ConsoleLogger.MinLevel = EventType.Trace; // Use EventType.Info for less detailed log output.
            using (var rhetosHost = RhetosHost.CreateFrom(rhetosAppPath, ConfigureRhetosHostForConsoleApp))
            {
                string appName = typeof(Program).Assembly.GetName().Name;
                var logger = rhetosHost.GetRootContainer().Resolve<ILogProvider>().GetLogger(appName);
                // Configure Hangfire to use Rhetos IoC container:
                GlobalConfiguration.Configuration.UseAutofacActivator(rhetosHost.GetRootContainer());
                // RhetosJobServerFactory will use Hangfire configuration from the Rhetos app:
                var rhetosJobServerFactory = rhetosHost.GetRootContainer().Resolve<RhetosJobServerFactory>();
                // Create and start a Hangfire jobs server:
                // Multiple servers may be created if needed, with different configurations, see CreateHangfireJobServer arguments.
                using (var hangfireJobServer = rhetosJobServerFactory.CreateHangfireJobServer())
                {
                    logger.Info("Started a Hangfire job server.");
                    Console.WriteLine("Press any key to stop the application.");
                    Console.ReadKey(true);
                    logger.Info("Stopping the Hangfire job server.");
                }
                logger.Info("Stopped the Hangfire job server.");
            }
            return 0;
        }

        private static void ConfigureRhetosHostForConsoleApp(IRhetosHostBuilder rhetosHostBuilder)
        {
            rhetosHostBuilder
              .UseBuilderLogProvider(new ConsoleLogProvider())
              .ConfigureContainer(containerBuilder =>
              {
                  containerBuilder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
                  containerBuilder.RegisterType<ConsoleLogProvider>().As<ILogProvider>();
              });
        }
    }
}
