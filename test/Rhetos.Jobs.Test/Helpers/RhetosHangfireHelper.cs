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

using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Rhetos.Jobs.Test
{
    public static class RhetosHangfireHelper
	{
        public static readonly IUserInfo JobsCreatedByUser = new TestUserInfo("TestJobsUser", "TestJobsMachine");

        /// <summary>
        /// Resolves IBackgroundJobs from DI and adds the new job by calling <see cref="BackgroundJobExtensions.EnqueueAction"/>.
        /// Returns Rhetos job IDs for provided actions.
        /// </summary>
        public static List<Guid> EnqueueActionJobs(List<(object ActionParameter, bool ExecuteInUserContext, bool OptimizeDuplicates, string Queue)> actions)
        {
            var rhetosJobIds = new List<Guid>();
            var log = new List<string>();
            using (var scope = RhetosProcessHelper.CreateScope(builder => builder
                .AddLogMonitor(log)
                .AddFakeUser(JobsCreatedByUser)))
            {
                var jobs = scope.Resolve<IBackgroundJobs>();

                foreach (var action in actions)
                {
                    jobs.EnqueueAction(action.ActionParameter, action.ExecuteInUserContext, action.OptimizeDuplicates, action.Queue);
                    rhetosJobIds.Add(GetLastJobId(log));
                }

                if (rhetosJobIds.Distinct().Count() != actions.Count)
                    throw new InvalidOperationException("Error in test setup, cannot detect all job IDs.");

                scope.CommitChanges();
            }
            return rhetosJobIds;
        }

        private static Guid GetLastJobId(List<string> log)
        {
            // Example: "[Trace] RhetosJobs: Enqueuing job.|JobId: 9a98ab0e-709f-4250-8602-34a89aab1d3a|User not specified|Rhetos.Jobs.ActionJobExecuter|Action: RhetosJobs.SimpleAction|Parameters: {\"Name\":\"testA\",\"TestId\":null}"
            string lastEntry = log.Last(entry => entry.Contains("RhetosJobs: Enqueuing job."));
            string jobId = _jobIdRegex.Match(lastEntry).Groups["guid"].Value;
            return Guid.Parse(jobId);
        }

        private static readonly Regex _jobIdRegex = new Regex(@"JobId: (?<guid>[\w-]+)");

        /// <summary>
        /// Returns dictionary: key is "Guid RhetosJobId", value is (long HangfireJobId, string JobArguments).
        /// </summary>
        public static Dictionary<Guid, (long HangfireJobId, string JobArguments)> ReadCreatedJobsFromDatabase(List<Guid> rhetosJobIds)
        {
            var hangfireJobIdByRhetosJobId = new Dictionary<Guid, (long HangfireJobId, string JobArguments)>();
            using (var scope = RhetosProcessHelper.CreateScope())
            {
                var sqlExecuter = scope.Resolve<ISqlExecuter>();
                foreach (Guid rhetosJobId in rhetosJobIds)
                {
                    sqlExecuter.ExecuteReader(
                        $@"SELECT Id, Arguments FROM HangFire.Job WITH (NOLOCK) WHERE Arguments LIKE '[[]""{{\""Id\"":\""{rhetosJobId}%'",
                        reader => hangfireJobIdByRhetosJobId.Add(rhetosJobId, (reader.GetInt64(0), reader.GetString(1))));
                }
            }
            return hangfireJobIdByRhetosJobId;
        }

        public static void WaitForJobsToComplete(List<long> hangfireJobIds)
        {
            var startTime = DateTime.Now;
            while (true)
            {
                var currentJobs = new List<string>();
                using (var scope = RhetosProcessHelper.CreateScope())
                {
                    string sql = "SELECT Id, StateName FROM HangFire.Job WHERE ExpireAt IS NULL"
                        + (hangfireJobIds != null ? $" AND Id IN ({string.Join(", ", hangfireJobIds)})" : "");

                    var sqlExecuter = scope.Resolve<ISqlExecuter>();
                    sqlExecuter.ExecuteReader(sql, reader => currentJobs.Add($"{reader.GetInt64(0)} {reader.GetString(1)}"));
                }

                if (!currentJobs.Any())
                    break;

                const int timeoutSeconds = 30;
                if (DateTime.Now - startTime > TimeSpan.FromSeconds(timeoutSeconds))
                    throw new InvalidOperationException($"Jobs not completed after {timeoutSeconds} seconds." +
                        $" States: {string.Join(", ", currentJobs)}.");

                Console.WriteLine("Waiting for current jobs to complete...");
                Thread.Sleep(400);
            }
        }
    }
}
