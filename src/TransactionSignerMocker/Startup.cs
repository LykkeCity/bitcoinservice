using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TransactionSignerMocker.Middleware;
using Swashbuckle.Swagger.Model;
using Microsoft.Extensions.PlatformAbstractions;
using Autofac.Extensions.DependencyInjection;
using Core.Settings;
using Microsoft.Extensions.Configuration;
using TransactionSignerMocker.Filters;
using TransactionSignerMocker.Binders;
using System.IO;
using AzureRepositories;

namespace TransactionSignerMocker
{
    public class Startup
    {
#if DEBUG
        const string SettingsBlobName = "bitcoinsettings.json";
#else
        const string SettingsBlobName = "globalsettings.json";
#endif

        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(env.ContentRootPath)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
               .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var settings = GeneralSettingsReader.ReadGeneralSettings<BaseSettings>(Configuration.GetConnectionString("Azure"), SettingsBlobName);

            services.AddMvc(o =>
            {
                o.Filters.Add(new HandleAllExceptionsFilterFactory());
            });

            services.AddSwaggerGen(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "TransactionSignerMocker_Api"
                });
                options.DescribeAllEnumsAsStrings();

                //Determine base path for the application.
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;

                //Set the comments path for the swagger json and ui.
                var xmlPath = Path.Combine(basePath, "TransactionSignerMocker.xml");
                options.IncludeXmlComments(xmlPath);
            });

            var builder = new AzureBinder().Bind(settings);
            builder.Populate(services);

            return new AutofacServiceProvider(builder.Build());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMiddleware<GlobalErrorHandlerMiddleware>();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUi("swagger/ui/index");
        }
    }
}
