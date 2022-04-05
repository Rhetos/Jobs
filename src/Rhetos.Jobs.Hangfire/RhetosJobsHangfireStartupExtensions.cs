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
using Rhetos.Extensibility;
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
        /// Calling this method is enough to allow creating background jobs in the current application.
        /// To start processing background jobs in the current application, call <see cref="UseRhetosHangfireServer(IApplicationBuilder, Action{BackgroundJobServerOptions}[])"/>,
        /// or use <see cref="RhetosJobServerFactory"/> to run the background jobs in a separate application.
        /// </remarks>
        public static RhetosServiceCollectionBuilder AddJobsHangfire(this RhetosServiceCollectionBuilder builder)
        {
            builder.ConfigureRhetosHost(
                (serviceProvider, rhetosHostBuilder) => rhetosHostBuilder.ConfigureContainer(
                    containerBuilder =>
                    {
                        containerBuilder.Register(context => context.Resolve<IConfiguration>().GetOptions<RhetosJobHangfireOptions>()).SingleInstance();
                        containerBuilder.RegisterType<BackgroundJobs>().As<IBackgroundJobs>().InstancePerLifetimeScope();
                        containerBuilder.RegisterType<RhetosHangfireJobs>().InstancePerLifetimeScope();
                        containerBuilder.RegisterGeneric(typeof(RhetosExecutionContext<,>)).InstancePerLifetimeScope();
                        containerBuilder.RegisterType<RhetosHangfireInitialization>().SingleInstance();
                        containerBuilder.RegisterType<RhetosJobServerFactory>().SingleInstance();
                        containerBuilder.RegisterType<JobServersCollection>().SingleInstance();

                        // Automatic update of recurring jobs is activated by the 'implementation' package,
                        // not the 'interface' package (Rhetos.Jobs.Abstractions), event though those classes
                        // are implemented in the interface package.
                        containerBuilder.RegisterType<RecurringJobsFromConfigurationOnDeploy>().As<IServerInitializer>();
                        containerBuilder.RegisterType<RecurringJobsFromConfigurationOnStartup>();
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
        /// <param name="configureOptions">
        /// Use this parameter to run multiple background job servers with different Hangfire options.
        /// If not provided, one background job server will be started with default settings.
        /// The Action may be null for any background job server; it uses app setting for <see cref="RhetosJobHangfireOptions"/> by default,
        /// </param>
        public static IApplicationBuilder UseRhetosHangfireServer(this IApplicationBuilder applicationBuilder, params Action<BackgroundJobServerOptions>[] configureOptions)
        {
            var rhetosHost = applicationBuilder.ApplicationServices.GetRequiredService<RhetosHost>();
            UseRhetosHangfireServer(rhetosHost, configureOptions);
            return applicationBuilder;
        }

        /// <summary>
        /// Starts background job processing in the current application's process.
        /// </summary>
        /// <remarks>
        /// It creates a new instance of Hangfire <see cref="BackgroundJobServer"/>.
        /// It uses app settings from <see cref="RhetosJobHangfireOptions"/> for <see cref="BackgroundJobServerOptions"/>.
        /// The created <see cref="BackgroundJobServer"/> will start processing background jobs immediately.
        /// <para>
        /// Remove this method call if the background jobs need to be processed in a separate application (e.g. a Windows service), instead of the current application.
        /// </para>
        /// </remarks>
        /// <param name="configureOptions">
        /// Use this parameter to run multiple background job servers with different Hangfire options.
        /// If not provided, one background job server will be started with default settings.
        /// The Action may be null for any background job server; it uses app setting for <see cref="RhetosJobHangfireOptions"/> by default,
        /// </param>
        public static void UseRhetosHangfireServer(RhetosHost rhetosHost, params Action<BackgroundJobServerOptions>[] configureOptions)
        {
            GlobalConfiguration.Configuration.UseAutofacActivator(rhetosHost.GetRootContainer());

            if (configureOptions.Length == 0)
                configureOptions = new Action<BackgroundJobServerOptions>[] { null };

            foreach (var configure in configureOptions)
            {
                var jobServers = rhetosHost.GetRootContainer().Resolve<JobServersCollection>();
                jobServers.CreateJobServer(configure);
            }
        }

        /// <summary>
        /// Recurring jobs can be specified in the application's settings.
        /// This method schedules the recurring background jobs from the configuration,
        /// and cancels any obsolete scheduled jobs when removed from configuration.
        /// </summary>
        /// <remarks>
        /// This method will not create the recurring jobs if the configuration option <see cref="RecurringJobsOptions.UpdateRecurringJobsFromConfigurationOnStartup"/> is disabled.
        /// </remarks>
        public static IApplicationBuilder UseRhetosJobsFromConfiguration(this IApplicationBuilder applicationBuilder)
        {
            var rhetosHost = applicationBuilder.ApplicationServices.GetRequiredService<RhetosHost>();
            RecurringJobsFromConfigurationOnStartup.Initialize(rhetosHost);
            return applicationBuilder;
        }
    }
}
