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
using Rhetos.Logging;
using System;
using System.IO;

namespace Rhetos.Jobs.Test
{
    /// <summary>
    /// Helper class for unit tests.
    /// </summary>
    public static class RhetosProcessHelper
    {
        /// <summary>
        /// Creates a thread-safe lifetime scope DI container to isolate unit of work in a separate database transaction.
        /// To commit changes to database, call <see cref="UnitOfWorkScope.CommitAndClose"/> at the end of the 'using' block.
        /// </summary>
        public static TransactionScopeContainer CreateScope(Action<ContainerBuilder> registerCustomComponents = null)
        {
            return ProcessContainer.CreateTransactionScopeContainer(registerCustomComponents);
        }

        /// <summary>
        /// Shared DI container to be reused between tests, to reduce initialization time for each test.
        /// Each test should create a child container with <see cref="CreateScope"/> to start a 'using' block.
        /// </summary>
        public static ProcessContainer ProcessContainer = new ProcessContainer(
            FindRhetosApplicationFolder(),
            null,
            configBuilder => configBuilder.SetHangfireTestConfiguration());

        /// <summary>
        /// Unit tests can be executed at different disk locations depending on whether they are run at the solution or project level, from Visual Studio or another utility.
        /// Therefore, instead of providing a simple relative path, this method searches for the main application location.
        /// </summary>
        private static string FindRhetosApplicationFolder()
        {
            const string testAppRelativePath = @".\test\TestApp\";
            var folder = new DirectoryInfo(Environment.CurrentDirectory);

            while (true)
            {
                string testAppFolder = Path.Combine(folder.FullName, testAppRelativePath);
                if (Directory.Exists(testAppFolder))
                {
                    folder = new DirectoryInfo(testAppFolder);
                    break;
                }

                if (folder.Parent == null)
                    throw new FrameworkException($"Cannot locate '{testAppRelativePath}' starting from '{Environment.CurrentDirectory}' or any parent folder.");

                folder = folder.Parent;
            }
            
            if (!IsValidRhetosServerDirectory(folder.FullName))
                throw new FrameworkException($"Cannot find Rhetos application in '{folder.FullName}'.");

            return folder.FullName;
        }

        private static bool IsValidRhetosServerDirectory(string path)
        {
            // Heuristics for recognizing Source\Rhetos project folder.
            return File.Exists(Path.Combine(path, @"Web.config"))
                && File.Exists(Path.Combine(path, @"bin\Rhetos.Utilities.dll"));
        }
    }
}
