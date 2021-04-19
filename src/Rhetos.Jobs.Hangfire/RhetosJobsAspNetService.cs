using Autofac.Integration.Wcf;
using Hangfire;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Rhetos.Jobs.Hangfire
{
	/// <summary>
	/// Initializes Hangfire server for background job processing in ASP.NET Rhetos applications.
	/// </summary>
	/// <remarks>
	/// The jobs server initialization is called automatically in a Rhetos web application (<see cref="IService"/> implementation).
	/// In other processes, for example CLI utilities or unit tests, use <see cref="RhetosJobServer"/> to create
	/// the Hangfire <see cref="BackgroundJobServer"/>, in order to start job processing in the current application.
	/// </remarks>
	public class RhetosJobsAspNetService : IService
	{
		private readonly RhetosJobHangfireOptions _options;

		public RhetosJobsAspNetService(RhetosJobHangfireOptions options)
		{
			_options = options;
		}

		/// <summary>
		/// This method is called automatically on Rhetos web application startup.
		/// </summary>
		public void Initialize()
		{
			if(_options.InitializeHangfireServer)
			{
				RhetosJobServer.ConfigureHangfireJobServers(AutofacHostFactory.Container, null);
				HangfireAspNet.Use(CreateHangfireServers);
			}
		}

		private static IEnumerable<IDisposable> CreateHangfireServers()
		{
			var server = RhetosJobServer.CreateHangfireJobServer();
			AspNetJobServers.Add(new WeakReference<BackgroundJobServer>(server));
			yield return server;
		}

		/// <summary>
		/// Hangfire background job servers that have been created for Hangfire-ASP.NET integration.
		/// </summary>
		/// <remarks>
		/// Using WeakReference to avoid interfering with BackgroundJobServer disposal, since this is a static field.
		/// </remarks>
		public static readonly ConcurrentBag<WeakReference<BackgroundJobServer>> AspNetJobServers = new ConcurrentBag<WeakReference<BackgroundJobServer>>();

		public void InitializeApplicationInstance(System.Web.HttpApplication context)
		{
			// No need for instance-specific initialization.
		}
	}
}
