# Rhetos.Jobs Release notes

## 1.3.0 (TO BE RELEASED)

* Bugfix: After a job fails, it will be executed again (retry) in the `default` queue, instead of the initially specified queueu.

## 1.2.0 (2023-06-01)

* Added `Rhetos:Jobs:Hangfire:AutomaticRetryAttempts` option to control how many times Hangfire will try to execute method if error occurs (default value is 10). Also, you can control the time between each unsuccessfull execution with `Rhetos:Jobs:Hangfire:DelaysInSeconds` option (if empty, Hangfire default exponential delay time will apply). 

## 1.1.0 (2022-02-14)

* Recurring jobs supported, specified by a [CRON expression](https://en.wikipedia.org/wiki/Cron#CRON_expression).
* The recurring jobs that executed a DSL Action can be specified in the application's configuration setting.

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
