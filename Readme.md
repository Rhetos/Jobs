# Rhetos.Jobs.Hangfire

Rhetos.Jobs.Hangfire is a plugin package for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides an implementation of asynchronous execution of background jobs.

Contents:

1. [Installation](#installation)
2. [Configuration](#configuration)
3. [Adding Hangfire Dashboard UI](#adding-hangfire-dashboard-ui)
4. [Usage](#usage)
5. [Running job server in unit tests and CLI utilities](#running-job-server-in-unit-tests-and-cli-utilities)
6. [Troubleshooting](#troubleshooting)
   1. [ThreadAbortException](#threadabortexception)

See [rhetos.org](http://www.rhetos.org/) for more information on Rhetos.

## Installation

Installing this package to a Rhetos web application:

1. Add "Rhetos.Jobs.Hangfire" NuGet package, available at the [NuGet.org](https://www.nuget.org/) on-line gallery.
2. In `Startup.ConfigureServices` method, extend the Rhetos services configuration (at `services.AddRhetosHost`) with: `.AddJobsHangfire()`

If you want to run the background jobs in the **Rhetos web application**:

1. In `Startup.Configure` method, add `app.UseRhetosHangfireServer(); // Start background job processing.`
2. If running the application on IIS, follow the instructions in section
   [Making ASP.NET Core application always running on IIS](https://docs.hangfire.io/en/latest/deployment-to-production/making-aspnet-app-always-running.html#making-asp-net-core-application-always-running-on-iis).

If you want to run the background jobs in a **separate application**, see instructions below
in section [Running job server in unit tests and CLI utilities](#running-job-server-in-unit-tests-and-cli-utilities).

## Configuration

Configuration of the plugin is done in `appsettings.json`, like in the following example (all parameters are optional).

```js
{
  "Rhetos": {
    "Jobs": {
      "Hangfire": {
        "InitializeHangfireServer": true, //If true Hangfire server will be initialized in Rhetos web application. Default value is true.
        "ProcessUserName": "hangfire-user", //UserName under which enqueued actions will be executed if action is not enqueued with executeInUserContext=true. If omitted then UserName of the account of the app pool user will be used.
        "Queues": ["default"], //Array of queue names which will be processed by this instance of Hangfire server. Default is '["default"]'.
        "CommandBatchMaxTimeout": 300, //Value is in seconds. Default value is 300. For usage of the option see Hangfire documentation.
        "SlidingInvisibilityTimeout": 300, //Value is in seconds. Default value is 300. For usage of the option see Hangfire documentation.
        "QueuePollInterval": 0, //Value is in seconds. Default value is 0. For usage of the option see Hangfire documentation.
        "UseRecommendedIsolationLevel": true, //Default value is true. For usage of the option see Hangfire documentation.
        "DisableGlobalLocks": true, //Default value is true. For usage of the option see Hangfire documentation.
        "WorkerCount": 2, //Default value is 2. For usage of the option see Hangfire documentation.
        "ShutdownTimeout": 15, //Value is in seconds. Default value is 15. For usage of the option see Hangfire documentation.
        "StopTimeout": 0, //Value is in seconds. Default value is 0. For usage of the option see Hangfire documentation.
        "SchedulePollingInterval": 15, //Value is in seconds. Default value is 15. For usage of the option see Hangfire documentation.
        "HeartbeatInterval": 30, //Value is in seconds. Default value is 30. For usage of the option see Hangfire documentation.
        "ServerTimeout": 300, //Value is in seconds. Default value is 300. For usage of the option see Hangfire documentation.
        "ServerCheckInterval": 300, //Value is in seconds. Default value is 300. For usage of the option see Hangfire documentation.
        "CancellationCheckInterval": 5 //Value is in seconds. Default value is 5. For usage of the option see Hangfire documentation.
      }
    }
  }
}
```

## Adding Hangfire Dashboard UI

See "Adding Dashboard UI" in Hangfire documentation:
<https://docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html#adding-dashboard-ui>

## Usage

In order to enqueue asynchronous execution of an action you have to:

* Demand from IOC container an instance of `IBackgroundJobs`.
* Create action parameters of an action you wish to enqueue for asynchronous execution.
* Call `EnqueueAction` method of `IBackgroundJobs` object. Method parameters are:
  * object action - Action which should be executed.
  * bool executeInUserContext - If true Action will be executed in context of the user which started the transaction in which Action was enqueued. Otherwise it will be executed in context of service account.
  * bool optimizeDuplicates - If true previous same Actions (same Action with same parameters) will be removed from queue.

Here is an example:

```c
Module Test
{
  Entity SomeEntity
  {
    SaveMethod
    {
      AfterSave EnqueueAsyncExecutionOfSomething
        '{
          foreach (var insertedItem in insertedNew)
          {
            var action = new Test.ProcessSomething { ItemId = insertedItem.Id };
            _backgroundJob.EnqueueAction(action, executeInUserContext: false, optimizeDuplicates: true);
          }
        }';
    }
    RepositoryUses _backgroundJob 'Rhetos.Jobs.IBackgroundJobs, Rhetos.Jobs.Abstractions';
  }
  
  Action ProcessSomething '(parameters, repository, userInfo) =>
  {
    // some code goes here
  }'
  {
    Guid ItemId;
  }
}
```

Enqueued actions will be executed asynchronously, immediately after the transaction in which they were enqueued is closed.
If the transaction is rolled back for any number of reasons, actions will not be enqueued and therefore not executed.

## Running job server in unit tests and CLI utilities

You can create a separate application to run the background jobs,
for example a Windows service or a CLI utility,
instead of running them directly in the Rhetos web application.
These utility applications will use Rhetos app's context and configuration
when executing the jobs.

Creating a "job runner" utility for your project:

1. Create the "job runner" console application and add a project reference to your Rhetos application.
2. Add the following text in the csproj file to suppress Rhetos build tasks in the job runner:
   ```xml
   <PropertyGroup>
     <RhetosBuild>false</RhetosBuild>
     <RhetosDeploy>false</RhetosDeploy>
   </PropertyGroup>
   ```
3. Add the following code as an example, modify as needed.

```cs
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
    /// This is a CLI utility for running background job separately for the Rhetos web app.
    /// </summary>
    class Program
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
```

Remarks:

* CreateHangfireJobServer supports parameter for configuring Hangfire.BackgroundJobServerOptions.
  The options are initialized from app settings (see RhetosJobHangfireOptions), and can be modified
  by this delegate.
* As an alternative to `rhetosJobServerFactory.CreateHangfireJobServer`, you can create and configure Hangfire.BackgroundJobServer
  directly, without automatically reading BackgroundJobServerOptions from app settings.
* You can test the job runner utility with LINQPad script that creates the jobs.

## Troubleshooting

### ThreadAbortException

*ThreadAbortException* can occur on application shutdown if there are some Hangfire background jobs still running.
Review the Hangfire documentation:
For console apps and Windows services, make sure to [Dispose BackgroundJobServer](https://docs.hangfire.io/en/latest/background-processing/processing-background-jobs.html) before exiting the application.
For web applications, reduce the issue with [Making ASP.NET application always running](https://docs.hangfire.io/en/latest/deployment-to-production/making-aspnet-app-always-running.html).
