using Rhetos.Jobs;
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace TestApp
{
    public class LongRunningJobExecuter : IJobExecuter<int>
    {
        private readonly ILogger _logger;
        private readonly int _instanceId = 0;
        private static int NextInstanceId = 0;

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