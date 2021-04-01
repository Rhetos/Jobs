using System.ComponentModel.Composition;
using Autofac;
using Rhetos.Utilities;

namespace Rhetos.Jobs
{
	[Export(typeof(Module))]
	public class AutofacConfiguration : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<ActionJobExecuter>();
			base.Load(builder);
		}
	}
}