using Autofac;
using Rhetos;
using Rhetos.MsSqlEf6.CommonConcepts;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace TestApp
{
    //[Export(typeof(Module))]
    public class MultiTenantAutofacModule : Module
    {
        public static IReadOnlyList<(string TenantName, string ConnectionString)> AllTenants { get; set; } =
        [
            ("Tenant1", "Data Source=localhost;Initial Catalog=RhetosJobs;Integrated Security=SSPI;"),
            ("Tenant2", "Data Source=localhost;Initial Catalog=RhetosJobs2;Integrated Security=SSPI;"),
        ];

        protected override void Load(ContainerBuilder builder)
        {
            ExecutionStage stage = builder.GetRhetosExecutionStage();

            if (stage.IsDatabaseUpdate || stage.IsApplicationInitialization)
                builder.Register(GetConnectionStringForDbUpdate).As<ConnectionString>().SingleInstance();
            else
            {
                builder.Register(GetConnectionStringForUser).As<ConnectionString>().InstancePerMatchingLifetimeScope(UnitOfWorkScope.ScopeName);
                builder.Register(GetEf6InitializationConnectionString).SingleInstance();
            }
        }

        const string dbUpdateTenantConfigurationKey = "Rhetos:DbUpdate:Tenant";
        const string runtimeInitializationConnectionStringConfigurationKey = "MultitenantCommonInitializationConnectionString";

        /// <summary>
        /// On deployment, admin can specify in configuration (e.g. in a local environment variable) the tenant name, or the database connection string.
        /// </summary>
        private static ConnectionString GetConnectionStringForDbUpdate(IComponentContext context)
        {
            // 1. If the dbupdate is called for a specific tenant, update the tenant's database.
            // For example, DbUpdate can be configured by setting the environment variable in command line before running rhetos dbupdate: SET Rhetos__DbUpdate__Tenant=...
            var configuration = context.Resolve<IConfiguration>();
            var tenant = configuration.GetValue<string>(dbUpdateTenantConfigurationKey);
            if (!string.IsNullOrEmpty(tenant))
                return GetTenantsConnectionString(tenant);

            // 2. Otherwise, use the default database, if provided.
            var sqlUtility = context.Resolve<ISqlUtility>();
            var rhetosConnectionString = new ConnectionString(configuration, sqlUtility);
            if (!string.IsNullOrEmpty(rhetosConnectionString))
                return rhetosConnectionString;

            throw new ArgumentException($"The database connection string is not specified. Please set the configuration option '{dbUpdateTenantConfigurationKey}' or '{ConnectionString.ConnectionStringConfigurationKey}'.");
        }

        /// <summary>
        /// In runtime, from user context we can get the tenant's connection string.
        /// </summary>
        private static ConnectionString GetConnectionStringForUser(IComponentContext context)
        {
            var user = context.Resolve<IUserInfo>();
            // TODO: Implement a custom tenant identification mechanism, for example a middleware that resolves the tenant by subdomain, HTTP header, JWT claim or cookie.
            var tenant = $"TENANT_FOR_{user.UserName}";
            return GetTenantsConnectionString(tenant);
        }

        private static ConnectionString GetTenantsConnectionString(string tenant)
        {
            // TODO: Implement a custom connection string storage, and retrieve the tenant's connection string here.
            //return new ConnectionString($"CONNECTION_STRING_FOR_{tenant}");

            if (DateTime.Now.Minute % 2 == 1)
                return new ConnectionString(AllTenants[0].ConnectionString);
            else
                return new ConnectionString(AllTenants[1].ConnectionString);
        }

        private Ef6InitializationConnectionString GetEf6InitializationConnectionString(IComponentContext context)
        {
            var configuration = context.Resolve<IConfiguration>();
            var sqlUtility = context.Resolve<ISqlUtility>();
            var rhetosConnectionString = new ConnectionString(ConnectionString.CreateConnectionString(configuration, sqlUtility, runtimeInitializationConnectionStringConfigurationKey));
            if (string.IsNullOrEmpty(rhetosConnectionString))
                throw new ArgumentException($"The database connection string is not specified. Please set the configuration option '{runtimeInitializationConnectionStringConfigurationKey}'.");
            return new Ef6InitializationConnectionString(rhetosConnectionString);
        }
    }
}
