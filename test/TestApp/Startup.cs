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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Rhetos;
using Rhetos.Jobs.Hangfire;
using System;

namespace TestApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRhetosHost(ConfigureRhetosHostBuilder)
                .AddAspNetCoreIdentityUser()
                .AddHostLogging()
                .AddJobsHangfire()
                .AddRestApi(o =>
                {
                    o.GroupNameMapper = (conceptInfo, controller, oldName) => "rhetos";
                });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TestApp", Version = "v1" });
                c.SwaggerDoc("rhetos", new OpenApiInfo { Title = "Rhetos REST API", Version = "v1" });
            });
        }

        private void ConfigureRhetosHostBuilder(IServiceProvider serviceProvider, IRhetosHostBuilder rhetosHostBuilder)
        {
            rhetosHostBuilder
                .ConfigureRhetosAppDefaults()
                .UseBuilderLogProviderFromHost(serviceProvider)
                .ConfigureConfiguration(cfg => cfg
                    .MapNetCoreConfiguration(Configuration)
                    .AddJsonFile("ConnectionString.local.json"))
                .ConfigureContainer(cb => cb
                    .RegisterType<LongRunningJobExecuter>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/rhetos/swagger.json", "Rhetos REST API"));
            }

            app.UseRhetosHangfireServer(); // Start background job processing in current application.

            var rhetosHost = app.ApplicationServices.GetRequiredService<RhetosHost>();
            var connectionString = rhetosHost.GetRootContainer().Resolve<Rhetos.Utilities.ConnectionString>();
            var jobStorageCollection = rhetosHost.GetRootContainer().Resolve<JobStorageCollection>();
            var jobStorage = jobStorageCollection.GetStorage(connectionString);
            app.UseHangfireDashboard(pathMatch: "/hangfire", storage: jobStorage);

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseRhetosRestApi();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHangfireDashboard(storage: jobStorage);
            });
        }
    }
}
