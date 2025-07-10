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

using Hangfire;
using Rhetos.Utilities;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// Provides the main Hangfire components from dependency injection container,
    /// as an alternative to using Hangfire's global static configuration and
    /// global static instances of classes <see cref="BackgroundJob"/> and <see cref="RecurringJob"/>
    /// </summary>
    /// <remarks>
    /// The problem with using global configuration and global instances is missing support for multitenant
    /// apps that have a database per tenant in a single application instance.
    /// </remarks>
    public class JobStorageProvider
    {
        private readonly JobStorageCollection _jobStorageCollection;
        private readonly ConnectionString _connectionString;

        public JobStorageProvider(JobStorageCollection jobStorageCollection, ConnectionString connectionString)
        {
            _jobStorageCollection = jobStorageCollection;
            _connectionString = connectionString;
        }

        public JobStorage GetStorage()
        {
            return _jobStorageCollection.GetStorage(_connectionString);
        }

        public RecurringJobManager GetRecurringJobManager()
        {
            return _jobStorageCollection.GetRecurringJobManager(_connectionString);
        }

        public BackgroundJobClient GetBackgroundJobClient()
        {
            return _jobStorageCollection.GetBackgroundJobClient(_connectionString);
        }
    }
}