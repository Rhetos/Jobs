# Rhetos.Jobs

Rhetos.Jobs is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides an implementation of asynchronous Action execution.

Contents:

1. [Installation and configuration](#installation-and-configuration)
2. [Usage](#usage)
3. [Running job server in unit tests and CLI utilities](#running-job-server-in-unit-tests-and-cli-utilities)
4. [Troubleshooting](#troubleshooting)
   1. [ThreadAbortException](#threadabortexception)

See [rhetos.org](http://www.rhetos.org/) for more information on Rhetos.

## Installation and configuration

To install this package to a Rhetos server, add it to the Rhetos server's *RhetosPackages.config* file
and make sure the NuGet package location is listed in the *RhetosPackageSources.config* file.

* The package ID is "**Rhetos.Jobs.Hangfire**".
  This package is available at the [NuGet.org](https://www.nuget.org/) online gallery.
  The Rhetos server can install the package directly from there, if the gallery is listed in *RhetosPackageSources.config* file.
* For more information, see [Installing plugin packages](https://github.com/Rhetos/Rhetos/wiki/Installing-plugin-packages).

Configuration of the plugin is done in `rhetos-app.settings.json`, like this (all parameters are optional):

```json
{
  "Rhetos": {
    "Jobs": {
      "Hangfire": {
        "ProcessUserName": "hangfire-user", //UserName under which enqueued actions will be executed if action is not enqueued with executeInUserContext=true. If omitted then UserName of the account of the app pool user will be used.
        "CommandBatchMaxTimeout": 300, //Value is in seconds. Default value is 300. For usage of the option see Hangfire documentation.
        "SlidingInvisibilityTimeout": 300, //Value is in seconds. Default value is 300. For usage of the option see Hangfire documentation.
        "QueuePollInterval": 0, //Value is in seconds. Default value is 0. For usage of the option see Hangfire documentation.
        "UseRecommendedIsolationLevel": true, //Default value is true. For usage of the option see Hangfire documentation.
        "DisableGlobalLocks": true //Default value is true. For usage of the option see Hangfire documentation.
      }
    }
  }
}
```

For applications with Global.asax (Rhetos v4), suppress the OWIN startup issue by adding
`<add key="owin:AutomaticAppStartup" value="false"/>` inside `appSettings` element in *Web.config* file.
See Hangfire documentation for more info in this issue: [Using Global.asax.cs file](https://docs.hangfire.io/en/latest/getting-started/aspnet-applications.html#using-global-asax-cs-file).

## Usage

In order to enqueue asynchronous execution of an action you have to:

* Demand from IOC container an instance of `IBackgroundJob`.
* Create action parameters of an action you wish to enqueue for asynchronous execution.
* Call `EnqueueAction` method of `IBackgroundJob` object. Method parameters are:
  * object action - Action which should be executed.
  * bool executeInUserContext - If true Action will be executed in context of the user which started the transaction in which Action was enqueued. Otherwise it will be executed in context of service account.
  * bool optimizeDuplicates - If true previous same Actions (same Action with same parameters) will be removed from queue.

Here is an example:

```c
Module Test
{
  Entity TheEntity
  {
    SaveMethod
    {
      AfterSave EnqueueAsyncExectuionOfSomething
      '
      {
        var action = new Test.SyncSomething();
        action.Ids = inserted.Select(x => new Guid?(x)).ToList();
        _backgroundJob.EnqueueAction(action, true, true);
      }';
      
    }
    RepositoryUses '_backgroundJob' 'Rhetos.Jobs.IBackgroundJob, Rhetos.Jobs.Abstractions';
  }
  
  Action SyncSomething '(parameters, repository, userInfo) =>
  {
    // some syncing code goes here
  }'
  {
    ListOf Guid Ids;
  }
}
```

Enqueued actions will be executed asynchronously, immediately after the transaction in which they were enqueued is closed. If the transaction is rolled back for any number of reasons, actions will not be enqueued and therefore not executed.

## Running job server in unit tests and CLI utilities

Hangfire job server is automatically started by Rhetos.Jobs.Hangfire in a Rhetos web application.

If you need to run the jobs processing server from another application that references the main Rhetos application's binaries, call `RhetosJobsService.InitializeJobServer(rootContainer);` at the application initialization.

For example, in a LINQPad script, add `RhetosJobsService.InitializeJobServer(GetRootContainer());` before the fist `using` statement, and add a method that returns the root container at the end of the script:

```cs
IContainer GetRootContainer()
{
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
