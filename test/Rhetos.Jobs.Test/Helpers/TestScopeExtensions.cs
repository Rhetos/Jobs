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
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Jobs.Test
{
    public static class TestScopeExtensions
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
