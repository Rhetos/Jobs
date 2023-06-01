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

using Rhetos.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Jobs
{
    /// <summary>
    /// Runtime configuration settings.
    /// </summary>
    [Options("Rhetos:Jobs")]
    public class RecurringJobsOptions
    {
        public bool UpdateRecurringJobsFromConfigurationOnDeploy { get; set; } = true;

        public bool UpdateRecurringJobsFromConfigurationOnStartup { get; set; } = true;

        public Dictionary<string, RecurringJobsDescription> Recurring { get; set; }

        public static RecurringJobsOptions FromConfiguration(IConfiguration configuration)
        {
            var options = configuration.GetOptions<RecurringJobsOptions>();
            options.Recurring = ReadJobsFromConfiguration(configuration);
            return options;
        }

        /// <summary>
        /// The property <see cref="Recurring"/> is not automatically deserialized by <see cref="IConfiguration.GetOptions"/>, because Dictionary type is not supported.
        /// </summary>
        private static Dictionary<string, RecurringJobsDescription> ReadJobsFromConfiguration(IConfiguration configuration)
        {
            var recurringKeyPrefix = ConfigurationProvider.GetKey<RecurringJobsOptions>(o => o.Recurring)
                + ConfigurationProviderOptions.ConfigurationPathSeparator;

            var recurringJobsNames = configuration.AllKeys
                .Where(key => key.StartsWith(recurringKeyPrefix, System.StringComparison.Ordinal))
                .Select(key => key
                    .Substring(recurringKeyPrefix.Length)
                    .Split(new[] { ConfigurationProviderOptions.ConfigurationPathSeparator }, System.StringSplitOptions.None)
                    .First())
                .Distinct()
                .ToList();

            var jobs = recurringJobsNames
                .Select(jobName => (jobName, jobDescription: configuration.GetOptions<RecurringJobsDescription>(recurringKeyPrefix + jobName)))
                .ToDictionary(job => job.jobName, job => job.jobDescription);

            return jobs;
        }
    }

    public class RecurringJobsDescription
    {
        public string CronExpression { get; set; }
        public string Action { get; set; }
        public string Queue { get; set; }
        public string RunAs { get; set; }
    }
}
