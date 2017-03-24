using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.ResolveAnything;
using AzureRepositories;
using BackgroundWorker.Binders;
using Common.IocContainer;
using Core.Settings;
using Microsoft.Extensions.Configuration;
using System.Runtime.Loader;
using Lykke.JobTriggers.Triggers;

namespace BackgroundWorker
{
    public class AppHost
    {
        public IConfigurationRoot Configuration { get; }

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
            BaseSettings settings;
#if DEBUG
            settings = GeneralSettingsReader.ReadGeneralSettingsLocal<BaseSettings>(Configuration.GetConnectionString("Settings"));
#else
            var generalSettings = GeneralSettingsReader.ReadGeneralSettings<GeneralSettings>(Configuration.GetConnectionString("Settings"));
            settings = generalSettings.BitcoinService;
#endif

            var containerBuilder = new AzureBinder().Bind(settings);
            var ioc = containerBuilder.Build();

            var triggerHost = new TriggerHost(new AutofacServiceProvider(ioc));

            triggerHost.ProvideAssembly(GetType().GetTypeInfo().Assembly);

            var end = new ManualResetEvent(false);

            AssemblyLoadContext.Default.Unloading += ctx =>
            {
                Console.WriteLine("SIGTERM recieved");
                triggerHost.Cancel();

                end.WaitOne();
            };

            triggerHost.Start().Wait();
            end.Set();
        }
    }
}
