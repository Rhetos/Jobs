using System.ComponentModel.Composition;
using Autofac;

namespace Rhetos.Jobs
{
	[Export(typeof(Module))]
	public class AutofacConfiguration : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<RhetosJobsService>().As<IService>();
			builder.RegisterType<JobScheduler>().As<IJobScheduler>().InstancePerLifetimeScope();
			builder.RegisterType<JobExecuter>().As<IJobExecuter>().InstancePerLifetimeScope();

			base.Load(builder);
		}
	}
}