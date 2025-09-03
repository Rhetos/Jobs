# Rhetos.Jobs Release notes

## 6.0.0 (2025-09-03)

* Update to .NET 8 and Rhetos 6.
* Support for *multitenant* applications with separate database per tenant on a single application.
  See the [Readme](Readme.md#multitenancy) for more info.

### Breaking changes

* RhetosJobServerFactory.CreateHangfireJobServer method now requires ILifetimeScope parameter.
  * In simple cases you can provide `rhetosHost.GetRootContainer()` for the parameter, but it is recommended
    to use `JobServersCollection.CreateJobServer` instead of `RhetosJobServerFactory` for
    more flexible configuration and more efficient management of multiple servers.
    `UseAutofacActivator` is no longer needed if migrating to JobServersCollection,
    see [JobRunner/Program.cs](Tools/JobRunner/Program.cs) for code sample.

## 5.4.0 (2025-04-30)

* Changes from release 1.3.0:
  * Bugfix: After a job fails, it will be executed again (retry) in the `default` queue, instead of the initially specified queue.

## 5.3.0 (2023-06-01)

* Added a recurring job option "RunAs". If specified, the job will run in a context of the specified Rhetos app user. See [Readme.md](Readme.md).
* Changes from release 1.2.0:
  * Added `Rhetos:Jobs:Hangfire:AutomaticRetryAttempts` option to control how many times Hangfire will try to execute method if error occurs (default value is 10). Also, you can control the time between each unsuccessful execution with `Rhetos:Jobs:Hangfire:DelaysInSeconds` option (if empty, Hangfire default exponential delay time will apply). 

## 5.2.0 (2022-07-06)

* Better error reporting for missing connection string.

## 5.1.0 (2022-04-08)

* Bugfix: Hangfire Dashboard startup error "Unable to find the required services", missing AddHangfire (issue #1).
* Support for anonymous user with executeInUserContext option.
* The application name in database connection string is extended with " Hangfire" suffix, to help with database debugging (configurable).

## 5.0.0 (2022-03-25)

### Breaking changes

1. Migrated from .NET Framework to .NET 5 and Rhetos 5.
2. Removed method `RhetosJobServer.ConfigureHangfireJobServers`.
   See [Readme.md](Readme.md) for new setup instructions.
3. Renamed class `RhetosJobServer` to `RhetosJobServerFactory`.

## 1.3.0 (2024-02-15)

*(Changes from v1.3.0 are not included in releases v5.0.0 - v5.3.0)*

* Bugfix: After a job fails, it will be executed again (retry) in the `default` queue, instead of the initially specified queue.

## 1.2.0 (2023-06-01)

*(Changes from v1.2.0 are not included in releases v5.0.0 - v5.2.0)*

* Added `Rhetos:Jobs:Hangfire:AutomaticRetryAttempts` option to control how many times Hangfire will try to execute method if error occurs (default value is 10). Also, you can control the time between each unsuccessful execution with `Rhetos:Jobs:Hangfire:DelaysInSeconds` option (if empty, Hangfire default exponential delay time will apply). 

## 1.1.0 (2022-02-14)

* Recurring jobs supported, specified by a [CRON expression](https://en.wikipedia.org/wiki/Cron#CRON_expression).
* The recurring jobs that execute a DSL Action can be specified in the application's configuration setting.

## 1.0.0 (2021-04-23)

* Asynchronous execution of background jobs:
  * *IBackgroundJobs* - Creates a new background job that will be executed after the current transaction is completed.
  * *IJobExecuter* - A custom background job executer.
* Helper methods for executing DSL Action in background.
* Integration with Hangfire (optional).
* If the unit of work (web request) that created the job does not complete successfully, the job will not be executed.
* Duplicate jobs, created within a single unit of work, can be aggregated or removed.
* Support for multiple jobs queues.
* Background jobs execution can be distributed over multiple applications, and execute on multiple types of application, such as the main web application, a console app or a windows service.
