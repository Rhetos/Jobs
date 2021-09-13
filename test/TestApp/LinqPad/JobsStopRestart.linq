<Query Kind="Program">
  <Reference Relative="..\bin\Autofac.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Autofac.dll</Reference>
  <Reference Relative="..\bin\EntityFramework.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\EntityFramework.dll</Reference>
  <Reference Relative="..\bin\EntityFramework.SqlServer.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\EntityFramework.SqlServer.dll</Reference>
  <Reference Relative="..\bin\Hangfire.AspNet.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Hangfire.AspNet.dll</Reference>
  <Reference Relative="..\bin\Hangfire.Autofac.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Hangfire.Autofac.dll</Reference>
  <Reference Relative="..\bin\Hangfire.Core.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Hangfire.Core.dll</Reference>
  <Reference Relative="..\bin\Hangfire.SqlServer.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Hangfire.SqlServer.dll</Reference>
  <Reference Relative="..\bin\NLog.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\NLog.dll</Reference>
  <Reference Relative="..\bin\Oracle.ManagedDataAccess.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Oracle.ManagedDataAccess.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Configuration.Autofac.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Configuration.Autofac.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Dom.DefaultConcepts.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Dom.DefaultConcepts.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Dom.DefaultConcepts.Interfaces.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Dom.DefaultConcepts.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Dom.Interfaces.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Dom.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Dsl.DefaultConcepts.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Dsl.DefaultConcepts.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Dsl.Interfaces.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Dsl.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Interfaces.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Jobs.Abstractions.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Jobs.Abstractions.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Jobs.Hangfire.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Jobs.Hangfire.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Logging.Interfaces.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Logging.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Persistence.Interfaces.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Persistence.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Processing.DefaultCommands.Interfaces.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Processing.DefaultCommands.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Processing.Interfaces.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Processing.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Security.Interfaces.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Security.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.TestCommon.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.TestCommon.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Utilities.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Rhetos.Utilities.dll</Reference>
  <Reference Relative="..\bin\TestApp.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\TestApp.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.AccountManagement.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.ComponentModel.Composition.dll</Reference>
  <Namespace>Autofac</Namespace>
  <Namespace>Oracle.ManagedDataAccess.Client</Namespace>
  <Namespace>Rhetos</Namespace>
  <Namespace>Rhetos.Configuration.Autofac</Namespace>
  <Namespace>Rhetos.Dom</Namespace>
  <Namespace>Rhetos.Dom.DefaultConcepts</Namespace>
  <Namespace>Rhetos.Dsl</Namespace>
  <Namespace>Rhetos.Dsl.DefaultConcepts</Namespace>
  <Namespace>Rhetos.Logging</Namespace>
  <Namespace>Rhetos.Persistence</Namespace>
  <Namespace>Rhetos.Security</Namespace>
  <Namespace>Rhetos.TestCommon</Namespace>
  <Namespace>Rhetos.Utilities</Namespace>
  <Namespace>System.Data.Entity</Namespace>
  <Namespace>System.DirectoryServices</Namespace>
  <Namespace>System.DirectoryServices.AccountManagement</Namespace>
  <Namespace>System.Runtime.Serialization.Json</Namespace>
  <Namespace>System.Xml.Serialization</Namespace>
  <Namespace>Rhetos.Jobs</Namespace>
  <Namespace>Rhetos.Jobs.Hangfire</Namespace>
  <Namespace>System.ComponentModel.Composition.Primitives</Namespace>
  <Namespace>System.ComponentModel.Composition</Namespace>
  <Namespace>Hangfire</Namespace>
</Query>

// This static instance of the BackgroundJobServer will keep executing the background jobs even after
// the LINQPad script finished, until the LINQPad process is closed (Menu: Query => Cancel All Threads and Reset).
static BackgroundJobServer BackgroundJobServer = CreateJobServer();

static BackgroundJobServer CreateJobServer()
{
    RhetosJobServer.Initialize(GetRootContainer(), builder => builder.RegisterType<TestJobExecuter>());
    return RhetosJobServer.CreateHangfireJobServer(); // We can create new BackgroundJobServer directly, instead of calling CreateHangfireJobServer, if a custom job configuration is needed.
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

class TestJobExecuter : IJobExecuter<object>
{
	public void Execute(object parameter)
	{
		Log($"[TestJobExecuter] {parameter}");
		Thread.Sleep(1000);
		Log($"[TestJobExecuter DONE] {parameter}");
	}
}

public static void Log(string message)
{
	Console.WriteLine($"{DateTime.Now:o} {message}");
}

string applicationFolder => Path.GetDirectoryName(Util.CurrentQueryPath); // Path to the Rhetos application, or any subfolder.

void Main()
{
	ConsoleLogger.MinLevel = EventType.Trace;

	ReportHangfireDatabaseJobs(-1).Dump("Existing jobs");
	long lastJobId = GetHangfireDatabaseLastJobId();

	using (var scope = ProcessContainer.CreateTransactionScopeContainer(applicationFolder))
	{
		var backgroundJobs = scope.Resolve<IBackgroundJobs>();

		for (int i = 0; i < 5; i++) // By default 2 runs in parallel for each background server.
			backgroundJobs.AddJob<TestJobExecuter, object>(i, false, null, null);

		scope.CommitChanges();
	}

	Thread.Sleep(100); // Wait enough for some jobs to start, but not to finish.

	ReportHangfireDatabaseJobs(lastJobId).Dump("Initially started jobs"); // Expected: 2 processing (default worker count), 3 pending.
	
	Log("===========  STOPPING ===========");

	BackgroundJobServer.SendStop();
	BackgroundJobServer.Dispose(); // Waits some time for running jobs to finish.

	ReportHangfireDatabaseJobs(lastJobId).Dump("After waiting for job server to stop"); // Expected: 2 completed, 3 pending.

	Log("===========  STOPPED ===========");

	Thread.Sleep(10000);

	Log("===========  ADD JOB ===========");

	using (var scope = ProcessContainer.CreateTransactionScopeContainer(applicationFolder))
	{
		var backgroundJobs = scope.Resolve<IBackgroundJobs>();

		backgroundJobs.AddJob<TestJobExecuter, object>(-1, false, null, null);

		scope.CommitChanges();
	}

	Thread.Sleep(10000);

	ReportHangfireDatabaseJobs(lastJobId).Dump("New job added. Background workers still stopped."); // Expected: 2 completed, 4 pending.

	Log("===========  RESTARTING ===========");

	BackgroundJobServer = RhetosJobServer.CreateHangfireJobServer();

	Thread.Sleep(10000);

	ReportHangfireDatabaseJobs(lastJobId).Dump("Background workers restarted."); // Expected: 6 completed.

	Log("===========  DONE ===========");
}

public long GetHangfireDatabaseLastJobId()
{
	using (var scope = ProcessContainer.CreateTransactionScopeContainer(applicationFolder))
	{
		string sql = "SELECT MAX(Id) FROM HangFire.Job WITH (nolock)";
		long lastJobId = 0; 
		var sqlExecuter = scope.Resolve<ISqlExecuter>();
		sqlExecuter.ExecuteReader(sql, reader => lastJobId = reader.IsDBNull(0) ? 0 : reader.GetInt64(0));
		return lastJobId;
	}
}

public List<object> ReportHangfireDatabaseJobs(long lastJobId)
{
	using (var scope = ProcessContainer.CreateTransactionScopeContainer(applicationFolder))
	{
		string sql = $"SELECT StateName, COUNT(*) FROM HangFire.Job WITH (nolock) WHERE Id > {lastJobId} GROUP BY StateName";
		var report = new List<object>();
		var sqlExecuter = scope.Resolve<ISqlExecuter>();
		sqlExecuter.ExecuteReader(sql,
			reader => report.Add(new
			{
				StateName = reader.GetString(0),
				Count = reader.GetInt32(1)
			}));
		return report;
	}
}