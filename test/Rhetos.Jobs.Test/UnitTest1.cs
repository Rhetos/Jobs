using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RhetosJobs;

namespace Rhetos.Jobs.Test
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void Happy()
		{
			using (var scope = RhetosProcessHelper.CreateScope())
			{
				var repository = scope.Resolve<Common.DomRepository>();
				repository.RhetosJobs.Happy.Execute(new Happy());
				scope.CommitChanges();
			}
		}

		[TestMethod]
		public void HappyWithWait()
		{
			using (var scope = RhetosProcessHelper.CreateScope())
			{
				var repository = scope.Resolve<Common.DomRepository>();
				repository.RhetosJobs.HappyWithWait.Execute(new HappyWithWait());
				scope.CommitChanges();
			}
		}
	}
}
