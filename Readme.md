# Rhetos.Jobs.Abstractions and Rhetos.Jobs.Hangfire

This repository contains two Rhetos plugin packages for asynchronous execution of background jobs:

1. **Rhetos.Jobs.Abstractions** - interfaces for asynchronous operations.
2. **Rhetos.Jobs.Hangfire** - an implementation option using Hangfire library.

Contents:

1. [Rhetos.Jobs.Abstractions](#rhetosjobsabstractions)
   1. [Asynchronously executing DSL Action in background](#asynchronously-executing-dsl-action-in-background)
   2. [Recurring jobs](#recurring-jobs)
2. [Rhetos.Jobs.Hangfire](#rhetosjobshangfire)
   1. [Installation](#installation)
   2. [Configuration](#configuration)
   3. [Recurring jobs on Hangfire](#recurring-jobs-on-hangfire)
   4. [Running job server in unit tests and CLI utilities](#running-job-server-in-unit-tests-and-cli-utilities)
   5. [Troubleshooting](#troubleshooting)
      1. [ThreadAbortException](#threadabortexception)

## Rhetos.Jobs.Abstractions

Rhetos.Jobs.Abstractions provides interfaces for asynchronous operations in Rhetos applications.
This packages should be referenced in Rhetos plugins that require asynchronous execution and background processing, but to not need to depend on a *specific library* that implements this feature.
The final application should reference an *implementation package* (for example Rhetos.Jobs.Hangfire) that contains implementation of those interfaces and runs the background jobs.

This package contains:

* Interface *IBackgroundJobs* - Creates a new background job that will be executed after the current transaction is completed.
* Interface *IJobExecuter* - Implement this interface to add a custom background job type.
  * Implementation *ActionJobExecuter* - Generic job executer for background jobs that are developed as a DSL Action.

### Asynchronously executing DSL Action in background

In order to enqueue asynchronous execution of a DSL Action you have to:

* Demand from IoC container an instance of `IBackgroundJobs`.
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
  
  Action ProcessSomething
    '(parameters, repository, userInfo) =>
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

### Recurring jobs

Recurring jobs can be configured to execute DSL Action in time periods, specified by [CRON expression](https://en.wikipedia.org/wiki/Cron).

Recurring jobs can be specified in the application settings, for example in rhetos-app.settings.json:

```json
{
  "Rhetos": {
    "Jobs": {
      "Recurring": {
        "RecurringJob1": {
          "CronExpression": "* * * * *",
          "Action": "SomeModule.RecurringAction1Minute"
        },
        "RecurringJob2": {
          "CronExpression": "0/2 * * * *",
          "Action": "SomeModule.RecurringAction2Minutes",
          "Queue": "default"
        }
      }
    }
  }
}
```

The jobs from the configuration are automatically applied to Hangfire on each Rhetos **database update**, and on each **application startup**.
This can be disabled by setting to `false` configuration options `Rhetos:Jobs:UpdateRecurringJobsFromConfigurationOnDeploy`
or `Rhetos:Jobs:UpdateRecurringJobsFromConfigurationOnStartup`.

As an alternative to the application settings, the recurring jobs can be added or removed programmatically
by using `IBackgroundJobs` interface, similar to the C# example above.

## Rhetos.Jobs.Hangfire

Rhetos.Jobs.Hangfire is a plugin package for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides an implementation of asynchronous execution of background jobs.

### Installation

To install this package to a Rhetos server, add it to the Rhetos server's *RhetosPackages.config* file
and make sure the NuGet package location is listed in the *RhetosPackageSources.config* file.

* The package ID is "**Rhetos.Jobs.Hangfire**".
  This package is available at the [NuGet.org](https://www.nuget.org/) online gallery.
  The Rhetos server can install the package directly from there, if the gallery is listed in *RhetosPackageSources.config* file.
* For more information, see [Installing plugin packages](https://github.com/Rhetos/Rhetos/wiki/Installing-plugin-packages).

If you want to run the background jobs in a **separate application**, see instructions below
in section [Running job server in unit tests and CLI utilities](#running-job-server-in-unit-tests-and-cli-utilities).

### Configuration

Configuration of the plugin is done in `rhetos-app.settings.json`, like in the following example (all parameters are optional).

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
        "CancellationCheckInterval": 5, //Value is in seconds. Default value is 5. For usage of the option see Hangfire documentation.
        "AutomaticRetryAttempts": 5 //Default value is 10. Added to GlobalJobFilters.Filters via AutomaticRetryAttribute. For usage of the AutomaticRetryAttribute see Hangfire documentation.
        "DelaysInSeconds": "1, 60, 3600" //Delays in seconds for retry jobs (i.e. "1, 60, 3600"). Default value is Hangfire default. For usage of the default algorithm see Hangfire documentation.
      }
    }
  }
}
```

For applications with Global.asax (Rhetos v4), suppress the OWIN startup issue by adding
`<add key="owin:AutomaticAppStartup" value="false"/>` inside `appSettings` element in *Web.config* file.
See Hangfire documentation for more info in this issue: [Using Global.asax.cs file](https://docs.hangfire.io/en/latest/getting-started/aspnet-applications.html#using-global-asax-cs-file).

### Recurring jobs on Hangfire

Hangfire 1.7 uses [NCrontab](https://github.com/atifaziz/NCrontab/blob/master/README.md) implementation or CRON expressions.

Time period shorter then a minute (mostly for testing purposes) might cause issues and require additional setup,
see [Hangfire recurring tasks under minute](https://stackoverflow.com/questions/38367398/hangfire-recurring-tasks-under-minute).

### Running job server in unit tests and CLI utilities

Hangfire job server is automatically started by Rhetos.Jobs.Hangfire in a Rhetos web application.

If you need to run the jobs processing server from another application that references the main Rhetos application's binaries,
call `RhetosJobServer.ConfigureHangfireJobServers` method at the application initialization,
then call `RhetosJobServer.CreateHangfireJobServer()` to create a Hangfire job server.
The Hangfire job server will start processing background jobs immediately.

For example, if you need to run background jobs in a **LINQPad script**, add the following code to the script.

```cs
static BackgroundJobServer BackgroundJobServer = CreateJobServer();

static BackgroundJobServer CreateJobServer()
{
    RhetosJobServer.ConfigureHangfireJobServers(GetRootContainer(), builder => builder.RegisterType<TestJobExecuter>());
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

### Troubleshooting

#### ThreadAbortException

*ThreadAbortException* can occur on application shutdown if there are some Hangfire background jobs still running.
Review the Hangfire documentation:
For console apps and Windows services, make sure to [Dispose BackgroundJobServer](https://docs.hangfire.io/en/latest/background-processing/processing-background-jobs.html) before exiting the application.
For web applications, reduce the issue with [Making ASP.NET application always running](https://docs.hangfire.io/en/latest/deployment-to-production/making-aspnet-app-always-running.html).
