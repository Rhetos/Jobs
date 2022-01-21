﻿/*
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

using Rhetos.Extensibility;
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Jobs.Hangfire
{
    [Export(typeof(IServerInitializer))]
    public class RecurringJobsFromConfigurationOnDeploy : IServerInitializer
    {
        private readonly RecurringJobsFromConfiguration _recurringJobsFromConfiguration;
        private readonly RecurringJobsOptions _recurringJobsOptions;
        private readonly ILogger _logger;

        public RecurringJobsFromConfigurationOnDeploy(RecurringJobsFromConfiguration recurringJobsFromConfiguration,
            RecurringJobsOptions recurringJobsOptions, ILogProvider logProvider)
        {
            _recurringJobsFromConfiguration = recurringJobsFromConfiguration;
            _recurringJobsOptions = recurringJobsOptions;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        public void Initialize()
        {
            if (!_recurringJobsOptions.UpdateRecurringJobsFromConfigurationOnDeploy)
            {
                _logger.Info($"{nameof(RecurringJobsOptions.UpdateRecurringJobsFromConfigurationOnDeploy)} is disabled.");
                return;
            }

            _recurringJobsFromConfiguration.UpdateHangfireJobsList();
        }

        public IEnumerable<string> Dependencies => Array.Empty<string>();
    }
}
