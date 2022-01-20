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

using Autofac;
using Autofac.Integration.Wcf;
using Rhetos.Logging;
using System.ComponentModel.Composition;
using System.Web;

namespace Rhetos.Jobs.Hangfire
{
    [Export(typeof(IService))]
    public class RecurringJobsFromConfigurationOnStartup : IService
    {
        private readonly ILogger _logger;
        private readonly RecurringJobsOptions _recurringJobsOptions;

        public RecurringJobsFromConfigurationOnStartup(ILogProvider logProvider, RecurringJobsOptions recurringJobsOptions)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _recurringJobsOptions = recurringJobsOptions;
        }

        public void Initialize()
        {
            if (!_recurringJobsOptions.UpdateRecurringJobsFromConfigurationOnStartup)
            {
                _logger.Info($"{nameof(RecurringJobsOptions.UpdateRecurringJobsFromConfigurationOnStartup)} is disabled.");
                return;
            }

            // IService initialization is executed in the root container scope. We cannot use the root scope
            // for database writes, because the database transaction is committed only when the scope is closed.

            // This code uses global Autofac DI container that is registered in Global.asax.cs in a Rhetos web app.
            var container = (IContainer)AutofacServiceHostFactory.Container;
            if (container != null)
            {
                Initialize(container);
            }
            else
            {
                _logger.Info(() => $"The recurring jobs are not updated, because root container is not available in {nameof(AutofacServiceHostFactory)}." +
                    $" If needed, initialize the jobs manually with static method {nameof(RecurringJobsFromConfigurationOnStartup)}.{nameof(RecurringJobsFromConfigurationOnStartup.Initialize)}.");
            }
        }

        public static void Initialize(IContainer container)
        {
            using (var scope = new TransactionScopeContainer(container))
            {
                var recurringJobsFromConfiguration = scope.Resolve<RecurringJobsFromConfiguration>();
                recurringJobsFromConfiguration.UpdateHangfireJobsList();
                scope.CommitChanges();
            }
        }

        public void InitializeApplicationInstance(HttpApplication context) { }
    }
}
