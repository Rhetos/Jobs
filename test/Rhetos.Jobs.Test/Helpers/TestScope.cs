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
using Common;
using Rhetos.Jobs.Hangfire;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;

namespace Rhetos.Jobs.Test
{
    /// <summary>
    /// Helper class for unit tests.
    /// </summary>
    public static class TestScope
    {
        /// <summary>
        /// Creates a thread-safe lifetime scope DI container (service provider)
        /// to isolate unit of work with a <b>separate database transaction</b>.
        /// To commit changes to database, call <see cref="IUnitOfWork.CommitAndClose"/> at the end of the 'using' block.
        /// </summary>
        public static IUnitOfWorkScope Create(Action<ContainerBuilder> registerCustomComponents = null)
        {
            return RhetosHost.CreateScope(registerCustomComponents);
        }

        /// <summary>
        /// Reusing a single shared static DI container between tests, to reduce initialization time for each test.
        /// Each test should create a child scope with <see cref="TestScope.Create"/> method to start a 'using' block.
        /// </summary>
        public static readonly RhetosHost RhetosHost = RhetosHost.CreateFrom(@"..\..\..\..\TestApp\bin\Debug\net8.0\TestApp.dll",
            hostBuilder => hostBuilder.ConfigureContainer(containerBuilder =>
            {
                containerBuilder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
                containerBuilder.RegisterInstance(new RhetosJobHangfireOptions {
                    SchedulePollingInterval = 1,
                    DelaysInSeconds = "1",
                    Queues = new[] { "default", BackgroundJobServer.TestQueue1Name },
                });
            }));

        /// <summary>
        /// Runs the test code in a new unit-of-work scope (see <see cref="Create"/>).
        /// The test code is provided with <see cref="ExecutionContext"/>, resolved from the scope.
        /// </summary>
        public static void Run(Action<ExecutionContext> test) => Run<ExecutionContext>(test);

        /// <summary>
        /// Runs the test code in a new unit-of-work scope (see <see cref="Create"/>).
        /// The test code is provided with component <typeparamref name="T"/>, resolved from the scope.
        /// </summary>
        public static void Run<T>(Action<T> test)
        {
            using (var scope = Create())
            {
                ConsoleLogger.MinLevel = EventType.Info;
                var component = scope.Resolve<T>();

                test(component);
            }
        }
    }
}
