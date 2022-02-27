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

using Rhetos.Logging;

namespace Rhetos.Jobs
{
    public class RecurringJobsFromConfigurationOnStartup
    {
        private readonly RecurringJobsFromConfiguration _recurringJobsFromConfiguration;
        private readonly RecurringJobsOptions _recurringJobsOptions;
        private readonly ILogger _logger;

        public RecurringJobsFromConfigurationOnStartup(RecurringJobsFromConfiguration recurringJobsFromConfiguration,
            RecurringJobsOptions recurringJobsOptions, ILogProvider logProvider)
        {
            _recurringJobsFromConfiguration = recurringJobsFromConfiguration;
            _recurringJobsOptions = recurringJobsOptions;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Recurring jobs can be specified in the application's settings.
        /// This method schedules the recurring background jobs from the configuration,
        /// and cancels any obsolete scheduled jobs when removed from configuration.
        /// </summary>
        /// <remarks>
        /// This method will not create the recurring jobs if the configuration option <see cref="RecurringJobsOptions.UpdateRecurringJobsFromConfigurationOnStartup"/> is disabled.
        /// </remarks>
        public static void Initialize(RhetosHost rhetosHost)
        {
            // Application startup initialization is executed in the root container scope. We cannot use the root scope
            // for database writes, because the database transaction is committed only when the scope is closed.
            using (var scope = rhetosHost.CreateScope())
            {
                var recurringJobsFromConfigurationOnStartup = scope.Resolve<RecurringJobsFromConfigurationOnStartup>();
                recurringJobsFromConfigurationOnStartup.Initialize();
                scope.CommitAndClose();
            }
        }

        public void Initialize()
        {
            if (!_recurringJobsOptions.UpdateRecurringJobsFromConfigurationOnStartup)
            {
                _logger.Info($"{nameof(RecurringJobsOptions.UpdateRecurringJobsFromConfigurationOnStartup)} is disabled.");
                return;
            }

            _recurringJobsFromConfiguration.UpdateJobs();
        }
    }
}
