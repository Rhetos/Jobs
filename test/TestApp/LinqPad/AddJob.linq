<Query Kind="Program">
  <Reference Relative="..\bin\Debug\net8.0\TestApp.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\TestApp.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\TestApp.deps.json">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\TestApp.deps.json</Reference>
  <Reference Relative="..\bin\Debug\net8.0\TestApp.runtimeconfig.json">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\TestApp.runtimeconfig.json</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.Jobs.Abstractions.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Rhetos.Jobs.Abstractions.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.Jobs.Hangfire.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Rhetos.Jobs.Hangfire.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\EntityFramework.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\EntityFramework.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\EntityFramework.SqlServer.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\EntityFramework.SqlServer.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\runtimes\win\lib\netcoreapp2.1\System.Data.SqlClient.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\runtimes\win\lib\netcoreapp2.1\System.Data.SqlClient.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Autofac.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Autofac.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Microsoft.CodeAnalysis.CSharp.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Microsoft.CodeAnalysis.CSharp.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Microsoft.CodeAnalysis.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Microsoft.CodeAnalysis.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Microsoft.Extensions.Localization.Abstractions.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Microsoft.Extensions.Localization.Abstractions.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Newtonsoft.Json.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Newtonsoft.Json.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\NLog.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\NLog.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.CommonConcepts.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Rhetos.CommonConcepts.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.Core.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Rhetos.Core.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.Core.DslParser.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Rhetos.Core.DslParser.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\Rhetos.Core.Integration.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\Rhetos.Core.Integration.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\runtimes\win-x64\native\sni.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\runtimes\win-x64\native\sni.dll</Reference>
  <Reference>..\runtimes\win\lib\net8.0\System.Diagnostics.EventLog.dll</Reference>
  <Reference>..\runtimes\win\lib\net8.0\System.Diagnostics.EventLog.Messages.dll</Reference>
  <Reference Relative="..\bin\Debug\net8.0\runtimes\win\lib\net8.0\System.Runtime.Caching.dll">C:\My Projects\RhetosPackages\Jobs\test\TestApp\bin\Debug\net8.0\runtimes\win\lib\net8.0\System.Runtime.Caching.dll</Reference>
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
  <Namespace>System.Threading.Tasks</Namespace>
  <IncludeAspNet>true</IncludeAspNet>
</Query>

void Main()
{
    ConsoleLogger.MinLevel = EventType.Info; // Use EventType.Trace for more detailed log.
	string rhetosHostAssemblyPath = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), @"..\bin\debug\net8.0\TestApp.dll");

	// Create a background job that executes a DSL action:
	using (var scope = LinqPadRhetosHost.CreateScope(rhetosHostAssemblyPath))
	{
		Thread.Sleep(3000);
		
		var jobs = scope.Resolve<IBackgroundJobs>();
		var action = new TestRhetosJobs.SimpleAction { Data = $"test{new Random().Next(99)}" };
		jobs.EnqueueAction(action, false, false);
		Console.WriteLine($"{DateTime.Now} job created: {action.Data}");
		scope.CommitAndClose();
	}

	// Test if the action has been executed. This action writes a log entry.
	// Run the TestApp application from Rhetos.Jobs.sln, to process the jobs.
	using (var scope = LinqPadRhetosHost.CreateScope(rhetosHostAssemblyPath))
	{
		var repository = scope.Resolve<Common.DomRepository>();

		for (int i = 0; i < 5; i++) // Retrying for 5 seconds to see the action results.
		{
			var entry = repository.Common.Log.Query(l => l.Action == "TestRhetosJobs.SimpleAction" && l.TableName == null)
				.OrderByDescending(l => l.Created)
				.FirstOrDefault();
			Console.WriteLine($"{entry.Created} {entry.Description}");
			Thread.Sleep(1000);			
		}
	}
}
