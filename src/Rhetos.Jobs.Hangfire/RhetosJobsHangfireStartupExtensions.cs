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
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Rhetos.Jobs;
using Rhetos.Jobs.Hangfire;
using Rhetos.Utilities;
using System;

namespace Rhetos
{
    /// <summary>
    /// Extension methods for setting up Rhetos.Jobs.Hangfire features.
    /// </summary>
    public static class RhetosJobsHangfireStartupExtensions
	{
        /// <summary>
        /// Adds required Rhetos components for creating background jobs, and background jobs processing.
        /// </summary>
        /// <remarks>
        /// To start processing background jobs in a current application, call <see cref="UseRhetosHangfireServer"/> 
        /// </remarks>
        public static RhetosServiceCollectionBuilder AddJobsHangfire(this RhetosServiceCollectionBuilder builder)
        {
            builder.ConfigureRhetosHost(
                (serviceProvider, rhetosHostBuilder) => rhetosHostBuilder.ConfigureContainer(
                    containerBuilder =>
                    {
                        containerBuilder.Register(context => context.Resolve<IConfiguration>().GetOptions<RhetosJobHangfireOptions>()).SingleInstance();
                        containerBuilder.RegisterType<BackgroundJobs>().As<IBackgroundJobs>().InstancePerLifetimeScope();
                        containerBuilder.RegisterGeneric(typeof(RhetosExecutionContext<,>)).InstancePerLifetimeScope();
                        containerBuilder.RegisterType<RhetosHangfireInitialization>().SingleInstance();
                        containerBuilder.RegisterType<RhetosJobServer>().SingleInstance();
                        containerBuilder.RegisterType<AspNetJobServers>().SingleInstance();
                    }));

            return builder;
        }

        /// <summary>
        /// Starts background job processing in the current application's process.
        /// </summary>
        /// <remarks>
        /// It creates a new instance of Hangfire <see cref="BackgroundJobServer"/>.
        /// It uses app settings from <see cref="RhetosJobHangfireOptions"/> for <see cref="BackgroundJobServerOptions"/>.
        /// The Hangfire BackgroundJobServer will start processing background jobs immediately.
        /// </remarks>
        public static IApplicationBuilder UseRhetosHangfireServer(this IApplicationBuilder applicationBuilder)
        {
            var rhetosHost = applicationBuilder.ApplicationServices.GetRequiredService<RhetosHost>();
            UseRhetosHangfireServer(rhetosHost);
            return applicationBuilder;
        }

        /// <summary>
        /// Starts background job processing in the current application's process.
        /// </summary>
        /// <remarks>
        /// It creates a new instance of Hangfire <see cref="BackgroundJobServer"/>.
        /// It uses app settings from <see cref="RhetosJobHangfireOptions"/> for <see cref="BackgroundJobServerOptions"/>.
        /// The created <see cref="BackgroundJobServer"/> will start processing background jobs immediately.
        /// </remarks>
        public static void UseRhetosHangfireServer(this RhetosHost rhetosHost)
        {
            RhetosJobServer.Initialize(rhetosHost);

            var jobServers = rhetosHost.GetRootContainer().Resolve<AspNetJobServers>();
            jobServers.CreateJobServer();
        }
    }
}
