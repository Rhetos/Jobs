using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Mvc;
using RhetosJobs;

namespace Rhetos.Jobs.Test
{
	[TestClass]
	public class UnitTest1
	{
		private const string RhetosUrl = "http://localhost/Rhetos/";
		
		[TestMethod]
		public void Happy()
		{
			var client = new RhetosRestClient.RestClient(RhetosUrl);
			client.ExecuteAction(new Happy());
		}

		[TestMethod]
		public void HappyWithWait()
		{
			var client = new RhetosRestClient.RestClient(RhetosUrl);
			client.ExecuteAction(new HappyWithWait());
		}
	}
}

namespace RhetosJobs
{
	public class Happy : BaseMvcModel
	{
	}

	public class HappyWithWait : BaseMvcModel
	{ }
}