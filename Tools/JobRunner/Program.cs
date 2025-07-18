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
using Rhetos;
using Rhetos.Jobs.Hangfire;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using TestApp;

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
            // First command-line argument is a relative path to the Rhetos application's assembly, for example "Bookstore.dll"
            string rhetosAppPath = args[0];
            ConsoleLogger.MinLevel = EventType.Trace; // Use EventType.Info for less detailed log output.
            using (var rhetosHost = RhetosHost.CreateFrom(rhetosAppPath, ConfigureRhetosHostForConsoleApp))
            {
                var container = rhetosHost.GetRootContainer();
                string appName = typeof(Program).Assembly.GetName().Name;
                var logger = container.Resolve<ILogProvider>().GetLogger(appName);
                var jobServers = container.Resolve<JobServersCollection>();

                // Create and start a Hangfire jobs server, with Hangfire configuration from the Rhetos app.
                // Multiple servers may be created if needed, with different configurations.
                // JobServersCollection manages the job server shutdown when RhetosHost is disposed.
                // FOR STANDARD APPS WITH A SINGLE APPLICATION DATABASE:
                jobServers.CreateJobServer(rhetosHost, configureOptions: null, connectionString: null);
                // FOR MULTITENANT APPLICATIONS WITH DATABASE PER TENANT, WITHOUT A GLOBAL CONNECTION STRING:
                //foreach (var tenant in MultiTenantAutofacModule.AllTenants)
                //    jobServers.CreateJobServer(rhetosHost, configureOptions: null, connectionString: tenant.ConnectionString);

                logger.Info("Started the Hangfire job servers.");
                logger.Info("Press any key to stop the application.");
                Console.ReadKey(true);
                logger.Info("Stopping the Hangfire job servers.");
            }
            Console.WriteLine("Stopped the Hangfire job servers.");
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
