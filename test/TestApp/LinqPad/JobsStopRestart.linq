<Query Kind="Program">
  <Reference Relative="..\bin\Debug\net8.0\TestApp.dll">..\bin\Debug\net8.0\TestApp.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\TestApp.deps.json">..\bin\Debug\net8.0\TestApp.deps.json</Reference>
  <Reference Relative="..\bin\Debug\net8.0\TestApp.runtimeconfig.json">..\bin\Debug\net8.0\TestApp.runtimeconfig.json</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.Jobs.Abstractions.dll">..\bin\Debug\net8.0\Rhetos.Jobs.Abstractions.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.Jobs.Hangfire.dll">..\bin\Debug\net8.0\Rhetos.Jobs.Hangfire.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\EntityFramework.dll">..\bin\Debug\net8.0\EntityFramework.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\EntityFramework.SqlServer.dll">..\bin\Debug\net8.0\EntityFramework.SqlServer.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\runtimes\win\lib\netcoreapp2.1\System.Data.SqlClient.dll">..\bin\Debug\net8.0\runtimes\win\lib\netcoreapp2.1\System.Data.SqlClient.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Autofac.dll">..\bin\Debug\net8.0\Autofac.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Microsoft.CodeAnalysis.CSharp.dll">..\bin\Debug\net8.0\Microsoft.CodeAnalysis.CSharp.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Microsoft.CodeAnalysis.dll">..\bin\Debug\net8.0\Microsoft.CodeAnalysis.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Microsoft.Extensions.Localization.Abstractions.dll">..\bin\Debug\net8.0\Microsoft.Extensions.Localization.Abstractions.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Newtonsoft.Json.dll">..\bin\Debug\net8.0\Newtonsoft.Json.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\NLog.dll">..\bin\Debug\net8.0\NLog.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.CommonConcepts.dll">..\bin\Debug\net8.0\Rhetos.CommonConcepts.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.Core.dll">..\bin\Debug\net8.0\Rhetos.Core.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.Core.DslParser.dll">..\bin\Debug\net8.0\Rhetos.Core.DslParser.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.Core.Integration.dll">..\bin\Debug\net8.0\Rhetos.Core.Integration.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\runtimes\win-x64\native\sni.dll">..\bin\Debug\net8.0\runtimes\win-x64\native\sni.dll</Reference>
  <Reference>..\runtimes\win\lib\net8.0\System.Diagnostics.EventLog.dll</Reference>
  <Reference>..\runtimes\win\lib\net8.0\System.Diagnostics.EventLog.Messages.dll</Reference>
  <Reference>..\bin\Debug\net8.0\runtimes\win\lib\net8.0\System.Runtime.Caching.dll</Reference>
  <Namespace>Autofac</Namespace>
  <Namespace>Rhetos</Namespace>
  <Namespace>Rhetos.Configuration.Autofac</Namespace>
  <Namespace>Rhetos.Dom</Namespace>
  <Namespace>Rhetos.Dom.DefaultConcepts</Namespace>
  <Namespace>Rhetos.Dsl</Namespace>
  <Namespace>Rhetos.Dsl.DefaultConcepts</Namespace>
  <Namespace>Rhetos.Logging</Namespace>
  <Namespace>Rhetos.Persistence</Namespace>
  <Namespace>Rhetos.Processing</Namespace>
  <Namespace>Rhetos.Processing.DefaultCommands</Namespace>
  <Namespace>Rhetos.Security</Namespace>
  <Namespace>Rhetos.Utilities</Namespace>
  <Namespace>System.Data.Entity</Namespace>
  <Namespace>System.DirectoryServices</Namespace>
  <Namespace>System.Runtime.Serialization.Json</Namespace>
  <Namespace>Hangfire</Namespace>
  <Namespace>Rhetos.Jobs</Namespace>
  <Namespace>Rhetos.Jobs.Hangfire</Namespace>
  <IncludeAspNet>true</IncludeAspNet>
</Query>

// IN CASE OF ERROR "The requested service 'TestJobExecuter' has not been registered.", RESTART THE LINQPad PROCESS.

void Main()
{
	var stopwatch = new Stopwatch();
	ConsoleLogger.MinLevel = EventType.Trace; // Use EventType.Trace for more detailed log.
	string rhetosAppPath = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), @"..\bin\debug\net8.0\TestApp.dll");
	bool test1 = true; // Restart the LINQPad process when changing this value.
	using (var rhetosHost = RhetosHost.CreateFrom(rhetosAppPath, ConfigureRhetosHost))
	{
		// Testing if application and database work:
		using (var scope = rhetosHost.CreateScope(cb => cb.RegisterType<ProcessUserInfo>().As<IUserInfo>()))
		{
			scope.Resolve<IUserInfo>().Report().Dump();
			scope.Resolve<Common.DomRepository>().Common.Claim.Query().Count().Dump();
		}

		// Create Hangfire jobs server:
		var container = rhetosHost.GetRootContainer();
		var rhetosJobServerFactory = container.Resolve<RhetosJobServerFactory>();
		using (var hangfireJobServer = rhetosJobServerFactory.CreateHangfireJobServer(container))
		{
			Console.WriteLine("Running a Hangfire job server.");
			
			if (test1)
				Test(rhetosHost, hangfireJobServer);
			else
				Test2(rhetosHost, hangfireJobServer); // Do not run after Test(). Not compatible.

			Console.WriteLine("Stopping the Hangfire job server.");
			Log("===========  Stopping the Hangfire job server ===========");
			stopwatch.Restart();
		}
		Log("===========  HANGFIRE JOB SERVER DISPOSED ===========");
		if (!test1)
			stopwatch.Elapsed.TotalSeconds.Dump(@"TotalSeconds. Expected: Disposing of hangfireJobServer in Main() should take 15 seconds waiting for this job to complete, see RhetosJobHangfireOptions.ShutdownTimeout.");
			//After that, and after rhetosHost is disposed in Main(), this job should run for 5 more seconds,
			//then report an error in LINQPad output when trying to complete the transaction (UnitOfWorkScope.CommitAndClose)
			//because the lifetime scope has already been disposed.");
	}
	Log("===========  RHETOS HOST DISPOSED ===========");
	if (!test1)
		"".Dump(@"After rhetosHost is disposed in Main(), this job should run for 5 more seconds,"
			+ " then report an error in LINQPad output when trying to complete the transaction (UnitOfWorkScope.CommitAndClose)"
			+ " because the lifetime scope has already been disposed.");
}

private static void ConfigureRhetosHost(IRhetosHostBuilder rhetosHostBuilder)
{
	rhetosHostBuilder
		.UseBuilderLogProvider(new ConsoleLogProvider())
		.ConfigureContainer(containerBuilder =>
		{
			// Console:
			containerBuilder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
			containerBuilder.RegisterType<ConsoleLogProvider>().As<ILogProvider>();
			// Test job executer:
			containerBuilder.RegisterType<TestJobExecuter>(); // IN CASE OF ERROR, RESTART THE LINQPad PROCESS TO AVOID: The requested service 'TestJobExecuter' has not been registered.
		});
}

class TestJobExecuter : IJobExecuter<object>
{
	public void Execute(object parameter)
	{
		Log($"[TestJobExecuter] {parameter.GetType()} {parameter}");
		if (parameter is long ms)
			Thread.Sleep((int)ms);
		else
			Thread.Sleep(1000);
		Log($"[TestJobExecuter DONE] {parameter.GetType()} {parameter}");
	}
}

public static void Log(string message)
{
	Console.WriteLine($"{DateTime.Now:o} {message}");
}

void Test2(RhetosHost rhetosHost, BackgroundJobServer hangfireJobServer)
{
	using (var scope = rhetosHost.CreateScope())
	{
		var backgroundJobs = scope.Resolve<IBackgroundJobs>();
		Log("===========  ADD JOB ===========");
		backgroundJobs.AddJob<TestJobExecuter, object>(20_000, false, null, null);
		scope.CommitAndClose();
	}
	Log("===========  SCOPE DISPOSED ===========");
	Thread.Sleep(100); // Wait enough for job to start, but not to finish.

	Log("===========  HANGFIRE JOB SERVER DISPOSING ===========");
}

void Test(RhetosHost rhetosHost, BackgroundJobServer hangfireJobServer)
{
	Log("===========  INITIAL STATE ===========");
	
	rhetosHost.ReportHangfireDatabaseJobs(-1).Dump("Existing jobs. Expected: All previous jobs succeeded. If not, delete all Hangfire tables to reset the database state.");
	long lastJobId = rhetosHost.GetHangfireDatabaseLastJobId();
	Log($"lastJobId: {lastJobId}");
	
	Log("===========  ADD JOBS ===========");

	using (var scope = rhetosHost.CreateScope())
	{
		var backgroundJobs = scope.Resolve<IBackgroundJobs>();

		for (int i = 0; i < 5; i++) // By default 2 runs in parallel for each background server, see RhetosJobHangfireOptions.WorkerCount.
			backgroundJobs.AddJob<TestJobExecuter, object>(i.ToString(), false, null, null);

		scope.CommitAndClose();
	}

	Thread.Sleep(100); // Wait enough for some jobs to start, but not to finish.

	rhetosHost.ReportHangfireDatabaseJobs(lastJobId).Dump("Initially started jobs. Expected: 2 processing (default worker count), 3 Enqueued. Remove the recurring jobs if there are more.");

	Log("===========  STOPPING ===========");
	
	hangfireJobServer.SendStop();
	hangfireJobServer.Dispose(); // Waits some time for running jobs to finish.

	rhetosHost.ReportHangfireDatabaseJobs(lastJobId).Dump("After waiting for job server to stop. Expected: 2 completed, 3 Enqueued.");

	Log("===========  STOPPED ===========");

	Thread.Sleep(10000);

	Log("===========  ADD JOB ===========");

	using (var scope = rhetosHost.CreateScope())
	{
		var backgroundJobs = scope.Resolve<IBackgroundJobs>();

		backgroundJobs.AddJob<TestJobExecuter, object>(123, false, null, null);

		scope.CommitAndClose();
	}

	Thread.Sleep(10000);

	rhetosHost.ReportHangfireDatabaseJobs(lastJobId).Dump("New job added. Background workers still stopped. Expected: 2 completed, 4 Enqueued.");

	Log("===========  RESTARTING ===========");

	var container = rhetosHost.GetRootContainer();
	var jobServerFactory = container.Resolve<RhetosJobServerFactory>();
	var connectionString = container.Resolve<ConnectionString>().Dump();
	using (var jobServer2 = jobServerFactory.CreateHangfireJobServer(container))
	{
		Thread.Sleep(10000);

		rhetosHost.ReportHangfireDatabaseJobs(lastJobId).Dump("Background workers restarted. Expected: 6 completed.");
	}

	Log("===========  DONE ===========");
}

static class RhetosHostExtensions
{
	public static long GetHangfireDatabaseLastJobId(this RhetosHost rhetosHost)
	{
		using (var scope = rhetosHost.CreateScope())
		{
			string sql = "SELECT MAX(Id) FROM HangFire.Job WITH (nolock)";
			long lastJobId = 0;
			var sqlExecuter = scope.Resolve<ISqlExecuter>();
			sqlExecuter.ExecuteReader(sql, reader => lastJobId = reader.IsDBNull(0) ? 0 : reader.GetInt64(0));
			return lastJobId;
		}
	}

	public static List<object> ReportHangfireDatabaseJobs(this RhetosHost rhetosHost, long lastJobId)
	{
		using (var scope = rhetosHost.CreateScope())
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
}
