using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using AzureRepositories;
using BitcoinApi.Binders;
using BitcoinApi.Filters;
using BitcoinApi.Middleware;
using Common.IocContainer;
using Core.Bitcoin;
using Core.OpenAssets;
using Core.Settings;
using LkeServices.Bitcoin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.Swagger.Model;
using Microsoft.AspNetCore.Http.Internal;

namespace BitcoinApi
{
    public class Startup
    {
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
            BaseSettings settings;
#if DEBUG
            settings = GeneralSettingsReader.ReadGeneralSettingsLocal<BaseSettings>(Configuration.GetConnectionString("Settings"));
#else
            var generalSettings = GeneralSettingsReader.ReadGeneralSettings<GeneralSettings>(Configuration.GetConnectionString("Settings"));
            settings = generalSettings.BitcoinApi;
#endif

            services.AddMvc(o =>
            {
                o.Filters.Add(new HandleAllExceptionsFilterFactory());
            });

            services.AddSwaggerGen(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "Bitcoin_Api"
                });
                options.DescribeAllEnumsAsStrings();

                //Determine base path for the application.
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;

                //Set the comments path for the swagger json and ui.
                var xmlPath = Path.Combine(basePath, "BitcoinApi.xml");
                options.IncludeXmlComments(xmlPath);                
                options.MapType<decimal>(() =>new Schema()
                {
                    Type = "number",
                    Format = "decimal"
                });
            });

            var builder = new AzureBinder().Bind(settings);
            builder.Populate(services);

            return new AutofacServiceProvider(builder.Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.Use(next => context =>
            {
                context.Request.EnableRewind();
                return next(context);
            });
            app.UseMiddleware<GlobalErrorHandlerMiddleware>();
            app.UseMiddleware<GlobalLogRequestsMiddleware>();            
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
