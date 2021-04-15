using Autofac;
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Jobs.Test
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder AddLogMonitor(this ContainerBuilder builder, List<string> log, EventType minLevel = EventType.Trace)
        {
            builder.RegisterInstance(new ConsoleLogProvider((eventType, eventName, message) =>
            {
                if (eventType >= minLevel)
                    log.Add("[" + eventType + "] " + (eventName != null ? (eventName + ": ") : "") + message());
            }))
                .As<ILogProvider>();
            return builder;
        }

        public static ContainerBuilder AddFakeUser(this ContainerBuilder builder, string username, string workstation)
        {
            return builder.AddFakeUser(new TestUserInfo(username, workstation));
        }

        public static ContainerBuilder AddFakeUser(this ContainerBuilder builder, IUserInfo userInfo)
        {
            builder.RegisterInstance(userInfo);
            return builder;
        }
    }
}
