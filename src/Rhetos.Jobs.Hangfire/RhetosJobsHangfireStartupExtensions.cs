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
using System.Linq;
using System.Reflection;

namespace Rhetos
{
    /// <summary>
    /// Extension methods for setting up Rhetos.Jobs.Hangfire features.
    /// </summary>
    public static class RhetosJobsHangfireStartupExtensions
	{
        /// <summary>
        /// Adds required Rhetos and HangFire components for creating background jobs, and background jobs processing.
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
                        containerBuilder.RegisterType<JobStorageCollection>().SingleInstance();
                        containerBuilder.RegisterType<JobStorageProvider>().InstancePerLifetimeScope();

                        // Automatic update of recurring jobs is activated by the 'implementation' package,
                        // not the 'interface' package (Rhetos.Jobs.Abstractions), event though those classes
                        // are implemented in the interface package.
                        containerBuilder.RegisterType<RecurringJobsFromConfigurationOnDeploy>().As<IServerInitializer>();
                        containerBuilder.RegisterType<RecurringJobsFromConfigurationOnStartup>();
                    }));

            AddComponentsForHangfireDashboard(builder);

            FixHangfireReflectionTypeLoadException();

            return builder;
        }

        private static void FixHangfireReflectionTypeLoadException()
        {
            // HACK: This workaround is a hack to avoid issues with combination of Hangfire.SqlServer v1.8.7, .NET 8, and System.Data.SqlClient v4.8.6.
            // Hangfire.BackgroundJob.Enqueue throws an internal exception: System.Reflection.ReflectionTypeLoadException: 'Unable to load one or more of the requested types. Could not load type 'SqlGuidCaster' from assembly 'System.Data.SqlClient, Version=4.6.1.6, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' because it contains an object field at offset 0 that is incorrectly aligned or overlapped by a non-object field.'
            // The exception is later caught and ignored in Hangfire code, but the indented feature is not used () witch hinders performance,
            // and unceasing runtime exceptions make debugging more complicated.
            // With this fix, the exception may still happen occasionally (not due to Hangfire), but less often.

            // TODO: Remove this method when the issue is corrected in Hangfire or .NET 8 or System.Data.SqlClient, or when System.Data.SqlClient is no longer used.

            Assembly hangfireAssembly = typeof(Hangfire.SqlServer.IPersistentJobQueue).Assembly;
            Type hangfireSqlCommandSet = hangfireAssembly.GetType("Hangfire.SqlServer.SqlCommandSet");
            FieldInfo hangfireSqlCommandSetTypeField = hangfireSqlCommandSet.GetField("SqlCommandSetType", BindingFlags.Static | BindingFlags.NonPublic);
            object hangfireSqlCommandSetTypeObject = hangfireSqlCommandSetTypeField.GetValue(null);
            var hangfireSqlCommandSetType = hangfireSqlCommandSetTypeObject as System.Collections.Concurrent.ConcurrentDictionary<Assembly, Type>;
            if (hangfireSqlCommandSetType == null)
                throw new InvalidOperationException($"Unexpected version of Hangfire. Check if this hack is needed in the new version. Maybe the method {nameof(FixHangfireReflectionTypeLoadException)} can now be removed.");

            Assembly sqlClientAssembly = typeof(System.Data.SqlClient.SqlConnection).Assembly;
            Type sqlClientSqlCommandSet = sqlClientAssembly.GetType("System.Data.SqlClient.SqlCommandSet");
            if (sqlClientSqlCommandSet == null)
                throw new InvalidOperationException($"Unexpected version of System.Data.SqlClient. Check if this hack is needed in the new version. Maybe the method {nameof(FixHangfireReflectionTypeLoadException)} can now be removed.");

            hangfireSqlCommandSetType.TryAdd(sqlClientAssembly, sqlClientSqlCommandSet);
        }

        private static void AddComponentsForHangfireDashboard(RhetosServiceCollectionBuilder builder)
        {
            // It seems that AddHangfire method call is not needed for job scheduling and processing. It is needed for HangFire dashboard,
            // because it depends on DI components, while other features use GlobalConfiguration if available (see method InitializeGlobalConfiguration).
            builder.Services.AddHangfire((serviceProvider, globalConfiguration) =>
            {
                // Calls Rhetos Hangfire initialization before returning the GlobalConfiguration for HangFire dashboard.
                // This is needed for apps that do not call UseRhetosHangfireServer() or have some other way of triggering RhetosHangfireInitialization.
                var rhetosHost = serviceProvider.GetRequiredService<RhetosHost>();
                var rhetosHangfireInitialization = rhetosHost.GetRootContainer().Resolve<RhetosHangfireInitialization>();
                rhetosHangfireInitialization.InitializeGlobalConfiguration();
            });
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
        /// The Action may be null for any background job server; it uses app setting for <see cref="RhetosJobHangfireOptions"/> by default.
        /// </param>
        public static IApplicationBuilder UseRhetosHangfireServer(this IApplicationBuilder applicationBuilder, params Action<BackgroundJobServerOptions>[] configureOptions)
        {
            return UseRhetosHangfireServer(applicationBuilder, configureOptions?.Select(co => ((string)null, co))?.ToArray());
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
        /// <param name="configurations">
        /// Use this parameter to run multiple background job servers with different Hangfire options.
        /// If not provided, one background job server will be started with default settings.
        /// The Action may be null for any background job server; it uses app setting for <see cref="RhetosJobHangfireOptions"/> by default.
        /// Connection string can be null, if there is global connection string available for the application (for multitenant app with database per tenant, specify the connection strings).
        /// </param>
        public static IApplicationBuilder UseRhetosHangfireServer(this IApplicationBuilder applicationBuilder, (string connectionString, Action<BackgroundJobServerOptions> configureOptions)[] configurations)
        {
            var rhetosHost = applicationBuilder.ApplicationServices.GetRequiredService<RhetosHost>();
            UseRhetosHangfireServer(rhetosHost, configurations);
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
        /// <param name="configurations">
        /// Use this parameter to run multiple background job servers with different Hangfire options.
        /// If not provided, one background job server will be started with default settings.
        /// The Action may be <see langword="null"/> for any background job server; it uses app setting for <see cref="RhetosJobHangfireOptions"/> by default.
        /// Connection string can be <see langword="null"/>, if there is global connection string available for the application (for multitenant app with database per tenant, specify the connection strings).
        /// </param>
        public static void UseRhetosHangfireServer(RhetosHost rhetosHost, params (string connectionString, Action<BackgroundJobServerOptions> configureOptions)[] configurations)
        {
            var container = rhetosHost.GetRootContainer();
            GlobalConfiguration.Configuration.UseAutofacActivator(container);

            if (configurations.Length == 0)
                configurations = [(null, null)];

            ConnectionString globalConnectionString = null;
            container.TryResolve(out globalConnectionString);

            foreach (var c in configurations)
            {
                if (globalConnectionString == null && c.connectionString == null)
                    throw new ArgumentException($"There is no global connection string registered and the connection string parameter is not specified in the '{nameof(configurations)}' method parameter.");

                var jobServers = container.Resolve<JobServersCollection>();
                jobServers.CreateJobServer(c.connectionString ?? globalConnectionString, c.configureOptions);
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
