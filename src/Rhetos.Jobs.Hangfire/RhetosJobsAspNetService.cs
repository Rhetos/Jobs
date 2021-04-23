/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

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
			if (_options.InitializeHangfireServer)
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

		/// <summary>
		/// Stops all Hangfire jobs servers that were created by this class.
		/// This method will wait the any currently running jobs to complete for duration of <see cref="RhetosJobHangfireOptions.ShutdownTimeout"/>,
		/// then return anyway if not completed (see Hangfire documentation on ShutdownTimeout).
		/// </summary>
		/// <remarks>
		/// The Shutdown method may be called from Application_End method in Global.asax.cs, but it is currently not needed in Global.asax
		/// because <see cref="RhetosJobsAspNetService"/> uses <see cref="HangfireAspNet"/> to manage job server instances.
		/// </remarks>
		public static void Shutdown()
        {
			if (AspNetJobServers == null)
				return;

			var servers = new List<BackgroundJobServer>();
			while (AspNetJobServers.TryTake(out var serverReference))
				if (serverReference != null && serverReference.TryGetTarget(out var server))
					servers.Add(server);

			// Sending stop signals to all server (in parallel), to avoid waiting for each independently (from Hangfire documentation).
			foreach (var server in servers)
				try { server.SendStop(); } catch { } // Ignoring possible issues if any server is already disposed on shutdown.

			// Waiting for each server to stop.
			foreach (var server in servers)
				try { server.Dispose(); } catch { } // Ignoring possible issues if any server is already disposed on shutdown.
		}
	}
}
