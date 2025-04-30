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
using Newtonsoft.Json;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Rhetos.Jobs.Test
{
    [TestClass]
	public class ActionJobExecuterTest
	{
		[TestMethod]
		public void OptimizeDuplicates()
        {
            var actions = new List<(object ActionParameter, bool ExecuteInUserContext, bool OptimizeDuplicates, bool AnonymousUser, string QueueName)>
            {
                (new TestRhetosJobs.SimpleAction { Data = "testA" }, false, true, false, null), // 0. Will be removed by duplicate 5.
				(new TestRhetosJobs.SimpleAction2 { Data = null }, false, true, false, null), // 1. Will be removed by duplicate 6.
				(new TestRhetosJobs.SimpleAction { Data = "testA" }, true, true, false, null), // 2. Different user context; not considered a duplicate.
				(new TestRhetosJobs.SimpleAction2 { Data = null }, true, true, false, null), // 3. Different user context; not considered a duplicate.
				(new TestRhetosJobs.SimpleAction { Data = "testB" }, false, true, false, null), // 4. Same action type but different parameter value; not a duplicate.
				(new TestRhetosJobs.SimpleAction { Data = "testA" }, false, true, false, null), // 5. Duplicate. The old one (0) should be removed. The duplicate jobs are intentionally not consecutive.
				(new TestRhetosJobs.SimpleAction2 { Data = null }, false, true, false, null), // 6. Duplicate. The old one (1) should be removed. The duplicate jobs are intentionally not consecutive.
				(new TestRhetosJobs.SimpleAction { Data = "testA" }, false, false, false, null), // 7. Duplicate, but with disabled optimizeDuplicates.
				(new TestRhetosJobs.SimpleAction2 { Data = null }, false, false, false, null), // 8. Duplicate, but with disabled optimizeDuplicates.
				(new TestRhetosJobs.SimpleAction { Data = "testA" }, false, true, true, null), // 9. Anonymous, will be removed by duplicate 93.
				(new TestRhetosJobs.SimpleAction { Data = null }, false, true, true, null), // 10. Anonymous, will be removed by duplicate 94.
				(new TestRhetosJobs.SimpleAction { Data = "testA" }, false, true, true, null), // 11 Duplicate. The old one (91) should be removed. The duplicate jobs are intentionally not consecutive.
				(new TestRhetosJobs.SimpleAction { Data = null }, false, true, true, null), // 12. Duplicate. The old one (92) should be removed. The duplicate jobs are intentionally not consecutive.
			};
            string expectedCreatedJobs = "11, 12, 2, 3, 4, 5, 6, 7, 8";

            var rhetosJobIds = RhetosHangfireHelper.EnqueueActionJobs(actions);

			// Because of "OptimizeDuplicates" argument above, some hangfire jobs should not be created.
			var hangfireJobs = RhetosHangfireHelper.ReadCreatedJobsFromDatabase(rhetosJobIds);

            var createdJobsIndexes = hangfireJobs.Select(hangfireJob => rhetosJobIds.IndexOf(hangfireJob.Key));
            Assert.AreEqual(expectedCreatedJobs, TestUtility.DumpSorted(createdJobsIndexes));
        }

        [TestMethod]
		public void ExecuteInUserContext()
        {
            var actions = new List<(object ActionParameter, bool ExecuteInUserContext, bool OptimizeDuplicates, bool AnonymousUser, string Queue)>
            {
                (new TestRhetosJobs.SimpleAction { Data = "ExecuteAsSystem " + Guid.NewGuid().ToString() }, false, false, false, null),
                (new TestRhetosJobs.SimpleAction { Data = "ExecuteAsUser " + Guid.NewGuid().ToString() }, true, false, false, null),
            };

            List<(string ContextInfo, string ActionData)> dbLog = ExecuteJobsReturnDbLog(actions);

            // Assert each jobs is executed with the correct user context:

            string expectedSystemUserReport = GetCurrentProcessUserReport();
            string expectedContextUserReport = "Rhetos:" + RhetosHangfireHelper.JobsCreatedByUser.UserName + ",Async job";
            var expectedJobsWithUserContext = new[]
            {
                "ExecuteAsSystem-" + expectedSystemUserReport,
                "ExecuteAsUser-" + expectedContextUserReport
            };

            var actualJobsWithUserContext = dbLog.Select(log => log.ActionData.Split(' ').First() + "-" + log.ContextInfo);

            Assert.AreEqual(
                TestUtility.DumpSorted(expectedJobsWithUserContext),
                TestUtility.DumpSorted(actualJobsWithUserContext));
        }

        [TestMethod]
        public void ExecuteInUserContextAnonymous()
        {
            var actions = new List<(object ActionParameter, bool ExecuteInUserContext, bool OptimizeDuplicates, bool AnonymousUser, string Queue)>
            {
                (new TestRhetosJobs.SimpleAction { Data = "ExecuteAsSystem " + Guid.NewGuid().ToString() }, false, false, true, null),
                (new TestRhetosJobs.SimpleAction { Data = "ExecuteAsAnonymous " + Guid.NewGuid().ToString() }, true, false, true, null),
            };

            List<(string ContextInfo, string ActionData)> dbLog = ExecuteJobsReturnDbLog(actions);

            // Assert each jobs is executed with the correct user context:

            string expectedSystemUserReport = GetCurrentProcessUserReport();
            string expectedAnonymousUserReport = "Rhetos:";
            var expectedJobsWithUserContext = new[]
            {
                "ExecuteAsSystem-" + expectedSystemUserReport,
                "ExecuteAsAnonymous-" + expectedAnonymousUserReport
            };
            var actualJobsWithUserContext = dbLog.Select(log => log.ActionData.Split(' ').First() + "-" + log.ContextInfo);

            Assert.AreEqual(
                TestUtility.DumpSorted(expectedJobsWithUserContext),
                TestUtility.DumpSorted(actualJobsWithUserContext));
        }

        private static List<(string ContextInfo, string ActionData)> ExecuteJobsReturnDbLog(List<(object ActionParameter, bool ExecuteInUserContext, bool OptimizeDuplicates, bool AnonymousUser, string Queue)> actions)
        {
            var rhetosJobIds = RhetosHangfireHelper.EnqueueActionJobs(actions);
            var hangfireJobs = RhetosHangfireHelper.ReadCreatedJobsFromDatabase(rhetosJobIds);
			var hangfireJobIds = hangfireJobs.Select(job => job.Value.HangfireJobId).ToList();

            // Read job results (the test action writes to Common.Log).

			RhetosHangfireHelper.WaitForJobsToComplete(hangfireJobIds);

            var actionData = actions.Select(action => ((TestRhetosJobs.SimpleAction)action.ActionParameter).Data).ToList();

            var dbLog = new List<(string ContextInfo, string ActionData)>();
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();

                var sql =
                    $@"SELECT ContextInfo, Description
					FROM Common.Log
						WHERE TableName IS NULL
							AND Action = 'TestRhetosJobs.SimpleAction'
							AND Description IN ({string.Join(", ", actionData.Select(context.SqlUtility.QuoteText))})";

                context.SqlExecuter.ExecuteReader(sql, reader => dbLog.Add((GetNullableString(reader, 0), GetNullableString(reader, 1))));
            }
            return dbLog;
        }

        private static string GetNullableString(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return null;
            else
                return reader.GetString(index);
        }

        private static string GetCurrentProcessUserReport()
        {
            using var scope = TestScope.Create();
            return "Rhetos:" + scope.Resolve<IUserInfo>().Report();
        }

		[TestMethod]
		public void UseCorrectQueueOnRetry()
		{
			DateTime testStart;
			using (var scope = TestScope.Create())
				testStart = SqlUtility.GetDatabaseTime(scope.Resolve<ISqlExecuter>());

            var actionTestQueue = new TestRhetosJobs.RetryingAction { Data = "testQueueJob " + Guid.NewGuid().ToString() };
            var actionDefaultQueue = new TestRhetosJobs.RetryingAction { Data = "defaultQueueJob " + Guid.NewGuid().ToString() };

            var actions = new List<(object ActionParameter, bool ExecuteInUserContext, bool OptimizeDuplicates, bool AnonymousUser, string Queue)>
            {
				(actionTestQueue, false, false, false, BackgroundJobServer.TestQueue1Name),
				(actionDefaultQueue, false, false, false, null),
			};

			var rhetosJobIds = RhetosHangfireHelper.EnqueueActionJobs(actions);
			var hangfireJobs = RhetosHangfireHelper.ReadCreatedJobsFromDatabase(rhetosJobIds);
            var hangfireJobIds = hangfireJobs.Select(job => job.Value.HangfireJobId).ToList();

            // Read job execution results:

            RhetosHangfireHelper.WaitForJobsToComplete(hangfireJobIds);

			string sql = $"SELECT Id, JobId, Data FROM HangFire.State WHERE JobId IN ({string.Join(", ", hangfireJobIds)}) AND Name = 'Enqueued'";

			var events = new List<(long Id, long JobId, string Data)>();
			using (var scope = TestScope.Create())
			{
				var sqlExecuter = scope.Resolve<ISqlExecuter>();
				sqlExecuter.ExecuteReader(sql, reader => events.Add((reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2))));
            }

			string report = string.Join(", ", actions.Select(action =>
			{
				string actionData = ((dynamic)action.ActionParameter).Data;
                var job = hangfireJobs.Values.Single(j => j.JobArguments.Contains(actionData));
				var eventQueueNames = events.Where(e => e.JobId == job.HangfireJobId).OrderBy(e => e.Id).Select(e => ParseQueueName(e.Data)).ToList();
				return actionData.Split(' ')[0] + ": " + string.Join(",", eventQueueNames);
            }));

            Assert.AreEqual(
				// For both jobs, the first execution and the second (retried) execution should be executed in the same queue.
                "testQueueJob: test-queue-1,test-queue-1, defaultQueueJob: default,default",
				report);
        }

        private static string ParseQueueName(string data)
        {
			// Example data: {"EnqueuedAt":"1706544248235","Queue":"test-queue-1"}
			return (string) ((dynamic)JsonConvert.DeserializeObject(data)).Queue;
        }
    }
}
