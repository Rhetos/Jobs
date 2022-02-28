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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Jobs.Hangfire;
using Rhetos.TestCommon;
using System.Linq;

namespace Rhetos.Jobs.Test
{
    [TestClass]
	public class RecurringJobsTest
	{
		[TestMethod]
		public void JobsFromConfiguration()
		{
			using (var scope = TestScope.Create())
			{
                var options = scope.Resolve<RecurringJobsOptions>();

                string expected =
                    "RecurringJob1: TestRhetosJobs.RecurringAction1|* * * * *|, " +
                    "RecurringJob2: TestRhetosJobs.RecurringAction2|0/2 * * * *|default";

                string actual = TestUtility.DumpSorted(
                    options.Recurring,
                    job => $"{job.Key}: {job.Value.Action}|{job.Value.CronExpression}|{job.Value.Queue}");

                Assert.AreEqual(expected, actual);
			}
		}
    }
}
