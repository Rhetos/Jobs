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

using Rhetos.Jobs;
using Rhetos.Logging;
using System.Threading;

namespace TestApp
{
    public class LongRunningJobExecuter : IJobExecuter<int>
    {
        private readonly ILogger _logger;
#pragma warning disable CA1805 // Do not initialize unnecessarily. The values are explicitly set to emphasize the initial values.
        private readonly int _instanceId = 0;
        private static int NextInstanceId = 0;
#pragma warning restore CA1805 // Do not initialize unnecessarily.

        public LongRunningJobExecuter(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _instanceId = Interlocked.Increment(ref NextInstanceId);
        }

        public void Execute(int duration)
        {
            _logger.Info($"Instance {_instanceId} starting for {duration} seconds.");
            Thread.Sleep(duration * 1000);
            _logger.Info($"Instance {_instanceId} finished after {duration} seconds.");
        }
    }
}