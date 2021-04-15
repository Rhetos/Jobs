using System.ComponentModel.Composition;
using Autofac;
using Rhetos.Utilities;

namespace Rhetos.Jobs.Hangfire
{
	[Export(typeof(Module))]
	public class AutofacConfiguration : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.Register(context => context.Resolve<IConfiguration>().GetOptions<RhetosJobHangfireOptions>()).SingleInstance();
			builder.RegisterType<RhetosJobsAspNetService>().As<IService>();
			builder.RegisterType<BackgroundJob>().As<IBackgroundJob>().InstancePerLifetimeScope();
			builder.RegisterGeneric(typeof(JobExecuter<,>)).InstancePerLifetimeScope();
			builder.RegisterType<RhetosHangfireInitialization>().SingleInstance();

			base.Load(builder);
		}
	}
}