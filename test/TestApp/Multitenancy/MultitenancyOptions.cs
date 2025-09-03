using Rhetos;
using System;
using System.Data.SqlClient;

namespace TestApp.Multitenancy
{
    [Options("TestApp:Multitenancy")]
    public class MultitenancyOptions
    {
        public bool Enabled { get; set; }

        /// <summary>
        /// Connection string for the central database that contains the table with a list of all tenants and their databases.
        /// </summary>
        public string TenantsConnectionString { get; set; }

        public void ValidateTenantsConnectionString()
        {
            string optionsPath = $"{OptionsAttribute.GetConfigurationPath<MultitenancyOptions>()}:{nameof(TenantsConnectionString)}";
            if (string.IsNullOrEmpty(TenantsConnectionString))
                throw new ArgumentException($"The central tenants database connection string is not configured. ('{optionsPath}')");

            try
            {
                using var connection = new SqlConnection(TenantsConnectionString);
                connection.Open();
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Invalid connection string for app initialization. Review the configuration option '{optionsPath}'.", e);
            }
        }
    }
}
