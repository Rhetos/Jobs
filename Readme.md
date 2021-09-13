# Rhetos.Jobs.Hangfire

Rhetos.Jobs.Hangfire is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides an implementation of asynchronous Action execution.

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

The steps above are enough for creating background jobs in the current application.
For executing background jobs there are two options: jobs can be executed in the current
application's process, or in a separate application (for example, a Windows service).

If you want to run the background jobs in the **current web application**:

1. In `Startup.Configure` method, add `app.UseRhetosHangfireServer(); // Start background job processing.`
2. If running the application on IIS, follow the instructions in section
   [Making ASP.NET Core application always running on IIS](https://docs.hangfire.io/en/latest/deployment-to-production/making-aspnet-app-always-running.html#making-asp-net-core-application-always-running-on-iis).

To run the background jobs in a **separate application**, see instructions below
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

To run the background jobs in separate application
that uses the Rhetos app's context and configuration:

```cs
TODO: test this code.
var rhetosHost = services.GetRequiredService<RhetosHost>();
RhetosJobServer.Initialize(rhetosHost);

var rhetosJobServer = rhetosHost.GetRootContainer().Resolve<RhetosJobServer>();
using (var jobServer = rhetosJobServer.CreateHangfireJobServer())
{
    Console.WriteLine("Running a Hangfire job server.");
    Console.WriteLine("Press any key to stop the application.");
    Console.ReadKey(true);
}
```


* CreateHangfireJobServer supports parameter for configuring Hangfire.BackgroundJobServerOptions.
  The options are initialized from app settings (see RhetosJobHangfireOptions), and can be modified
  by this delegate.
* As an alternative to `rhetosJobServer.CreateHangfireJobServer`, you can create Hangfire.BackgroundJobServer
  directly, without automatically reading BackgroundJobServerOptions from app settings.

```cs
using (var jobServer = new BackgroundJobServer(new BackgroundJobServerOptions()))
{
    Console.WriteLine("Running a Hangfire job server.");
    Console.WriteLine("Press any key to stop the application.");
    Console.ReadKey(true);
}
```

```cs
TODO: test this code.
TODO: create an instance of the referenced Rhetos application's IHost and bind BackgroundJobServerOptions serverOptions from IHost.

using (var jobServer = new BackgroundJobServer(serverOptions))
{
    Console.WriteLine("Running a Hangfire job server.");
    Console.WriteLine("Press any key to stop the application.");
    Console.ReadKey(true);
}
```


TODO:
--- OLD:

Hangfire job server is automatically started by Rhetos.Jobs.Hangfire in a Rhetos web application.

If you need to run the jobs processing server from another application that references the main Rhetos application's binaries,
call `RhetosJobServer.Initialize` method at the application initialization,
then call `RhetosJobServer.CreateHangfireJobServer()` to create a Hangfire job server.
The Hangfire job server will start processing background jobs immediately.

For example, if you need to run background jobs in a **LINQPad script**, add the following code to the script.

```cs
static BackgroundJobServer BackgroundJobServer = CreateJobServer();

static BackgroundJobServer CreateJobServer()
{
    TODO: Update documentation to match new methods.
    RhetosJobServer.Initialize(GetRootContainer(), builder => builder.RegisterType<TestJobExecuter>());
    return RhetosJobServer.CreateHangfireJobServer();
}

static IContainer GetRootContainer()
{
    string applicationFolder = Path.GetDirectoryName(Util.CurrentQueryPath); // Path to the Rhetos application, or any subfolder.
    using (var scope = ProcessContainer.CreateTransactionScopeContainer(applicationFolder))
    {
        var processContainerField = typeof(ProcessContainer).GetField("_singleContainer", BindingFlags.NonPublic | BindingFlags.Static);
        var processContainer = (ProcessContainer)processContainerField.GetValue(null);

        var containerField = typeof(ProcessContainer).GetField("_rhetosIocContainer", BindingFlags.NonPublic | BindingFlags.Instance);
        var container = (Lazy<IContainer>)containerField.GetValue(processContainer);

        return container.Value;
    }
}
```

## Troubleshooting

### ThreadAbortException

*ThreadAbortException* can occur on application shutdown if there are some Hangfire background jobs still running.
Review the Hangfire documentation:
For console apps and Windows services, make sure to [Dispose BackgroundJobServer](https://docs.hangfire.io/en/latest/background-processing/processing-background-jobs.html) before exiting the application.
For web applications, reduce the issue with [Making ASP.NET application always running](https://docs.hangfire.io/en/latest/deployment-to-production/making-aspnet-app-always-running.html).
