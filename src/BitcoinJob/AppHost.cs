using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.ResolveAnything;
using AzureRepositories;
using BackgroundWorker.Binders;
using Common.IocContainer;
using Core.Settings;
using LkeServices.Triggers;
using Microsoft.Extensions.Configuration;

namespace BackgroundWorker
{
    public class AppHost
    {
        public IConfigurationRoot Configuration { get; }

#if DEBUG
        const string SettingsBlobName = "bitcoinsettings.json";
#else
        const string SettingsBlobName = "globalsettings.json";
#endif

        public AppHost()
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public void Run()
        {
            var settings = GeneralSettingsReader.ReadGeneralSettings<BaseSettings>(Configuration.GetConnectionString("Azure"), SettingsBlobName);

            var containerBuilder = new AzureBinder().Bind(settings);
            var ioc = containerBuilder.Build();

            var triggerHost = new TriggerHost(new AutofacServiceProvider(ioc));

            triggerHost.ProvideAssembly(GetType().GetTypeInfo().Assembly);

            triggerHost.StartAndBlock();
        }
    }
}
