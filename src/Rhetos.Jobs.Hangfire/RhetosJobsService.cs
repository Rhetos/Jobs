using System;
using Autofac;
using Autofac.Integration.Wcf;
using Hangfire;

namespace Rhetos.Jobs.Hangfire
{
	/// <summary>
	/// Initializes Hangfire server that processes jobs in a Rhetos applications.
	/// </summary>
	/// <remarks>
	/// The jobs server initialization is called automatically in a Rhetos web application (<see cref="IService"/>).
	/// In other processes, for example CLI utilities or unit tests, call <see cref="InitializeJobServer"/> to start job processing
	/// within the current application process.
	/// </remarks>
	public class RhetosJobsService : IService
	{
		/// <summary>
		/// Call this method from CLI utilities or unit tests to start a job processing server withing the current application process.
		/// </summary>
		public static void InitializeJobServer(ILifetimeScope container)
		{
			_rootContainer = container;
			var hangfireInitialization = container.Resolve<RhetosHangfireInitialization>();
			hangfireInitialization.InitializeGlobalConfiguration();
			HangfireAspNet.Use(() => hangfireInitialization.GetHangfireServers(_rootContainer));
		}

        /// <summary>
        /// This method is called automatically on Rhetos web application startup.
        /// </summary>
        void IService.Initialize()
		{
			InitializeJobServer(AutofacHostFactory.Container);
		}

		void IService.InitializeApplicationInstance(System.Web.HttpApplication context)
		{
		}

		private static ILifetimeScope _rootContainer;

		public static TransactionScopeContainer CreateScope(Action<ContainerBuilder> customizeScope)
		{
			if (_rootContainer == null)
				throw new InvalidOperationException($"{nameof(RhetosJobsService)} not initialized. Call {nameof(RhetosJobsService)}.{nameof(InitializeJobServer)} first.");

			return new TransactionScopeContainer((IContainer)_rootContainer, customizeScope);
		}
	}
}
