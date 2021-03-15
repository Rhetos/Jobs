using System;
using System.ComponentModel.Composition;
using Autofac;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos.Jobs
{
	[Export(typeof(Module))]
	public class AutofacConfiguration : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<RhetosJobsService>().As<IService>();
			builder.RegisterType<TaskSheduler>().InstancePerLifetimeScope();
			builder.RegisterType<TaskExecuter>().InstancePerLifetimeScope();

			base.Load(builder);
		}
	}
}