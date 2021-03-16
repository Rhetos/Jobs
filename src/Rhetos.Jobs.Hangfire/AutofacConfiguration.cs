using System.ComponentModel.Composition;
using Autofac;

namespace Rhetos.Jobs.Hangfire
{
	[Export(typeof(Module))]
	public class AutofacConfiguration : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<RhetosJobsService>().As<IService>();
			builder.RegisterType<BackgroundJob>().As<IBackgroundJob>().InstancePerLifetimeScope();
			builder.RegisterType<JobExecuter>().As<IJobExecuter>().InstancePerLifetimeScope();

			base.Load(builder);
		}
	}
}