# Rhetos.Jobs.Abstractions and Rhetos.Jobs.Hangfire

This documentation describes installation and usage on Rhetos v5.
For documentation on older version of this plugin for **Rhetos v4**, see the previous release branch
[Readme.md](https://github.com/Rhetos/Jobs/blob/rhetos-4/Readme.md).

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
   4. [Adding Hangfire Dashboard UI](#adding-hangfire-dashboard-ui)
   5. [Running job server in a separate CLI application](#running-job-server-in-a-separate-cli-application)
   6. [Multitenancy](#multitenancy)
   7. [Troubleshooting](#troubleshooting)
      1. [ThreadAbortException](#threadabortexception)
3. [How to contribute](#how-to-contribute)
   1. [Building and testing the source code](#building-and-testing-the-source-code)

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
    RepositoryUses _backgroundJob 'Rhetos.Jobs.IBackgroundJobs';
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

Recurring jobs can be specified in the application settings, for example in appsettings.json:

```json
{
  "Rhetos": {
    "Jobs": {
      "Recurring": {
        "RecurringJob1": {
          "CronExpression": "0 3 * * 6", // Run at 3 AM each Saturday.
          "Action": "SomeModule.RecurringActionWeekly3AM"
        },
        "RecurringJob2": {
          "CronExpression": "0 0/2 * * *", // Run every 2 hours.
          "Action": "SomeModule.RecurringAction2Hours",
          "Queue": "default",
          "RunAs": "SomeUserName" // Name from Common.Principal.
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

Installing this package to a Rhetos application:

1. Add "Rhetos.Jobs.Hangfire" NuGet package, available at the [NuGet.org](https://www.nuget.org/) on-line gallery.
2. In `Startup.ConfigureServices` method, extend the Rhetos services configuration (at `services.AddRhetosHost`) with: `.AddJobsHangfire()`

If you want to run the background jobs in the **Rhetos web application**:

1. In `Startup.Configure` method, add:
   ```cs
   app.UseRecurringJobsFromConfiguration(); // Initialize recurring jobs.
   app.UseRhetosHangfireServer(); // Start background job processing in current application.
   ```
2. If running the application on IIS, follow the instructions in section
   [Making ASP.NET Core application always running on IIS](https://docs.hangfire.io/en/latest/deployment-to-production/making-aspnet-app-always-running.html#making-asp-net-core-application-always-running-on-iis).

If you want to run the background jobs in a **separate application**, see instructions below
in section [Running job server in unit tests and CLI utilities](#running-job-server-in-unit-tests-and-cli-utilities).

### Configuration

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
        "CancellationCheckInterval": 5, //Value is in seconds. Default value is 5. For usage of the option see Hangfire documentation.
        "AutomaticRetryAttempts": 3, //Default value is 10. Added to GlobalJobFilters.Filters via AutomaticRetryAttribute. For usage of the AutomaticRetryAttribute see Hangfire documentation.
        "DelaysInSeconds": "1, 60, 3600" //Delays in seconds for retry jobs (i.e. "1, 60, 3600"). Default value is Hangfire default. For usage of the default algorithm see Hangfire documentation.
      }
    }
  }
}
```

### Recurring jobs on Hangfire

Hangfire 1.7 uses [NCrontab](https://github.com/atifaziz/NCrontab/blob/master/README.md) implementation or CRON expressions.

Time period shorter then a minute (mostly for testing purposes) might cause issues and require additional setup,
see [Hangfire recurring tasks under minute](https://stackoverflow.com/questions/38367398/hangfire-recurring-tasks-under-minute).

### Adding Hangfire Dashboard UI

See "Adding Dashboard UI" in Hangfire documentation:
<https://docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html#adding-dashboard-ui>

### Running job server in a separate CLI application

You can create a separate application to run the background jobs,
for example a Windows service or a CLI utility,
instead of running them directly in the Rhetos web application.
These utility applications will use Rhetos app's context and configuration
when executing the jobs.

**Creating** a "job runner" utility for your project:

1. Create the "job runner" console application
2. Add a project reference to your Rhetos application.
   * Alternatively, the job runner may be used without the build reference to the main application,
     but in that case you might need to override the job runner's deps.json file with the main app's deps.json file
     to avoid "Could not load file or assembly" exception.
3. Add the following text in the csproj file to suppress Rhetos build tasks in the job runner:
   ```xml
   <PropertyGroup>
     <RhetosBuild>false</RhetosBuild>
     <RhetosDeploy>false</RhetosDeploy>
   </PropertyGroup>
   ```
4. Copy the content of `Program` class from demo JobRunner app:
   [Program.cs](https://github.com/Rhetos/Jobs/blob/master/Tools/JobRunner/Program.cs).

If needed, you can customize Hangfire **configuration** for job runner to be different from the main app:

* `CreateHangfireJobServer()` method in Program.cs supports configuring `Hangfire.BackgroundJobServerOptions`.
  The options are automatically initialized from the main app settings (see `RhetosJobHangfireOptions` class),
  and then modified by this delegate, for example:
  ```cs
  rhetosJobServerFactory.CreateHangfireJobServer(o => o.Queues = new[] { "priority-queue" })
  ````
* As an alternative to using `CreateHangfireJobServer()`, you can create and configure `Hangfire.BackgroundJobServer`
  directly, without automatically reading `BackgroundJobServerOptions` from app settings.

Testing:

* You can test the job runner utility with a LINQPad script that creates the jobs,
  for example see [AddJob.linq](https://github.com/Rhetos/Jobs/blob/master/test/TestApp/LinqPad/AddJob.linq).

### Multitenancy

In a multitenant application architecture with a single application and a separate database per tenant, there is no global database configuration available for hangfire.
In that case, the application initialization should create a separate Hangfire Server instance and a separate Hangfire Dashboard for each tenant.

For example, in a web application, use the following code in Program.cs or Startup.cs and modify it to match your application.
For more info see [TestApp/Startup.cs](test/TestApp/Startup.cs), and the [Multitenancy](https://github.com/Rhetos/Rhetos/wiki/Multitenancy) on the Rhetos wiki.

```cs
// Program.cs or Startup.cs:
...
var hangfireDatabases = GetHangfireDatabases(app);
foreach (var hangfireDatabase in hangfireDatabases)
{
    app.UseRecurringJobsFromConfiguration(hangfireDatabase.ConnectionString); // Initialize recurring jobs.
    app.UseRhetosHangfireServer(null, hangfireDatabase.ConnectionString); // Start background job processing in current application.
    app.UseHangfireDashboard(pathMatch: $"/hangfire-{hangfireDatabase.TenantName}", storage: hangfireDatabase.JobStorage);
}
...
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    ...
    foreach (var hangfireDatabase in hangfireDatabases)
        endpoints.MapHangfireDashboard(storage: hangfireDatabase.JobStorage);
});
...

private static List<(string TenantName, string ConnectionString, JobStorage JobStorage)> GetHangfireDatabases(IApplicationBuilder app)
{
    var rhetosHost = app.ApplicationServices.GetRequiredService<RhetosHost>();
    var jobStorageCollection = rhetosHost.GetRootContainer().Resolve<JobStorageCollection>();
    return MultiTenantAutofacModule.AllTenants // Replace this line with custom code to get the list of tenant databases in your application.
        .Select(tenant => (tenant.TenantName, tenant.ConnectionString, jobStorageCollection.GetStorage(tenant.ConnectionString)))
        .ToList();
}
```

If you are developing a CLI application that only runs Hangfire jobs, use the following code to start a separate Hangfire server for each tenant's database.
For more info see [JobRunner/Program.cs](Tools/JobRunner/Program.cs).

```cs
var jobServers = rhetosHost.GetRootContainer().Resolve<JobServersCollection>();
foreach (var tenant in MultiTenantAutofacModule.AllTenants)
    jobServers.CreateJobServer(rhetosHost, configureOptions: null, connectionString: tenant.ConnectionString);
```

### Troubleshooting

#### ThreadAbortException

*ThreadAbortException* can occur on application shutdown if there are some Hangfire background jobs still running.
Review the Hangfire documentation:
For console apps and Windows services, make sure to [Dispose BackgroundJobServer](https://docs.hangfire.io/en/latest/background-processing/processing-background-jobs.html) before exiting the application.
For web applications, reduce the issue with [Making ASP.NET application always running](https://docs.hangfire.io/en/latest/deployment-to-production/making-aspnet-app-always-running.html).

#### Multitenancy issues

In a multitenant app with a separate database per tenant, where there is no single global database, any of the following errors might occur if the Hangfire initialization expects the single global database:

* "Unable to resolve the type 'Rhetos.Utilities.ConnectionString' because the lifetime scope it belongs in can't be located."
* "The connection string is not specified in this method call, and there is no global connection string available."
* "Current JobStorage instance has not been initialized yet"

In case of those errors, see [Multitenancy](#multitenancy) section above for configuring the app with multiple databases.

## How to contribute

Contributions are very welcome. The easiest way is to fork this repo, and then
make a pull request from your fork. The first time you make a pull request, you
may be asked to sign a Contributor Agreement.
For more info see [How to Contribute](https://github.com/Rhetos/Rhetos/wiki/How-to-Contribute) on Rhetos wiki.

### Building and testing the source code

* Note: This package is already available at the [NuGet.org](https://www.nuget.org/) online gallery.
  You don't need to build it from source in order to use it in your application.
* To build the package from source, run `Clean.bat`, `Build.bat` and `Test.bat`.
* For the test script to work, you need to create an empty database and
  a settings file `test\TestApp\ConnectionString.local.json`
  with the database connection string (configuration key "ConnectionStrings:RhetosConnectionString").
* The build output is a NuGet package in the "Install" subfolder.
