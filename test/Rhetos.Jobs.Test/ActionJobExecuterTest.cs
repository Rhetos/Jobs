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
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Jobs.Test
{
    [TestClass]
	public class ActionJobExecuterTest
	{
		[TestMethod]
		public void OptimizeDuplicates()
        {
            var actions = new List<(object ActionParameter, bool ExecuteInUserContext, bool OptimizeDuplicates)>
            {
                (new TestRhetosJobs.SimpleAction { Data = "testA" }, false, true), // 0. Will be removed by duplicate 5.
				(new TestRhetosJobs.SimpleAction2 { Data = null }, false, true), // 1. Will be removed by duplicate 6.
				(new TestRhetosJobs.SimpleAction { Data = "testA" }, true, true), // 2. Different user context; not considered a duplicate.
				(new TestRhetosJobs.SimpleAction2 { Data = null }, true, true), // 3. Different user context; not considered a duplicate.
				(new TestRhetosJobs.SimpleAction { Data = "testB" }, false, true), // 4. Same action type but different parameter value; not a duplicate.
				(new TestRhetosJobs.SimpleAction { Data = "testA" }, false, true), // 5. Duplicate. The old one (0) should be removed. The duplicate jobs are intentionally not consecutive.
				(new TestRhetosJobs.SimpleAction2 { Data = null }, false, true), // 6. Duplicate. The old one (1) should be removed. The duplicate jobs are intentionally not consecutive.
				(new TestRhetosJobs.SimpleAction { Data = "testA" }, false, false), // 7. Duplicate, but with disabled optimizeDuplicates.
				(new TestRhetosJobs.SimpleAction2 { Data = null }, false, false), // 8. Duplicate, but with disabled optimizeDuplicates.
			};
            string expectedCreatedJobs = "2, 3, 4, 5, 6, 7, 8";

            var rhetosJobIds = RhetosHangfireHelper.EnqueueActionJobs(actions);

			// Because of "OptimizeDuplicates" argument above, some hangfire jobs should not be created.
			var hangfireJobs = RhetosHangfireHelper.ReadCreatedJobsFromDatabase(rhetosJobIds);

            var createdJobsIndexes = hangfireJobs.Select(hangfireJob => rhetosJobIds.IndexOf(hangfireJob.Key));
            Assert.AreEqual(expectedCreatedJobs, TestUtility.DumpSorted(createdJobsIndexes));
        }

        [TestMethod]
		public void ExecuteInUserContext()
		{
			DateTime testStart;
			using (var scope = TestScope.Create())
				testStart = SqlUtility.GetDatabaseTime(scope.Resolve<ISqlExecuter>());

			var actions = new List<(object ActionParameter, bool ExecuteInUserContext, bool OptimizeDuplicates)>
			{
				(new TestRhetosJobs.SimpleAction { Data = "ExecuteAsSystem " + Guid.NewGuid().ToString() }, false, false),
				(new TestRhetosJobs.SimpleAction { Data = "ExecuteAsUser " + Guid.NewGuid().ToString() }, true, false),
			};

			var rhetosJobIds = RhetosHangfireHelper.EnqueueActionJobs(actions);
			var hangfireJobs = RhetosHangfireHelper.ReadCreatedJobsFromDatabase(rhetosJobIds);

			// Read job results (the test action writes to Common.Log).

			RhetosHangfireHelper.WaitForJobsToComplete(hangfireJobs.Values.ToList());

			var actionData = actions.Select(action => ((TestRhetosJobs.SimpleAction)action.ActionParameter).Data).ToList();

			var dbLog = new List<(string ContextInfo, string ActionData)>();
			string systemUserReport;
			using (var scope = TestScope.Create())
			{
				var sql =
					$@"SELECT ContextInfo, Description
					FROM Common.Log
						WHERE TableName IS NULL
							AND Action = 'TestRhetosJobs.SimpleAction'
							AND Description IN ({string.Join(", ", actionData.Select(name => SqlUtility.QuoteText(name)))})";

				var sqlExecuter = scope.Resolve<ISqlExecuter>();
				sqlExecuter.ExecuteReader(sql, reader => dbLog.Add((reader.GetString(0), reader.GetString(1))));

				systemUserReport = scope.Resolve<IUserInfo>().Report();
			}

			// Assert each jobs is executed with the correct user context:

			var expectedJobsWithUserContext = new[]
			{
				"ExecuteAsSystem-Rhetos:" + systemUserReport,
				"ExecuteAsUser-Rhetos:" + RhetosHangfireHelper.JobsCreatedByUser.UserName + ",Async job"
			};
			var actualJobsWithUserContext = dbLog.Select(log => log.ActionData.Split(' ').First() + "-" + log.ContextInfo);

			Assert.AreEqual(
				TestUtility.DumpSorted(expectedJobsWithUserContext),
				TestUtility.DumpSorted(actualJobsWithUserContext));
		}
    }
}
