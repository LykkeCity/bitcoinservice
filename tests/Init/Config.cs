using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureRepositories;
using Common;
using Common.Log;
using Core.Bitcoin;
using Core.Settings;
using LkeServices;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Bitcoin.Tests
{
    [SetUpFixture]
    public class Config
    {
        public static IServiceProvider Services { get; set; }
        public static ILog Logger => Services.GetService<ILog>();

        private BaseSettings ReadSettings()
        {
            try
            {
                var json = File.ReadAllText(@"..\settings\settings.json");
                if (string.IsNullOrWhiteSpace(json))
                {

                    return null;
                }
                BaseSettings settings = json.DeserializeJson<BaseSettings>();

                return settings;
            }
            catch (Exception)
            {
                return null;
            }
        }


        [OneTimeSetUp]
        public void Initialize()
        {
            var settings = GeneralSettingsReader.ReadGeneralSettings<BaseSettings>("UseDevelopmentStorage=true",
                "bitcoinsettings.json");

            var log = new LogToConsole();

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(settings);
            builder.RegisterInstance(log).As<ILog>();
            builder.RegisterInstance(new RpcConnectionParams(settings));
            builder.BindAzure(settings, log);
            builder.BindCommonServices();

            Services = new AutofacServiceProvider(builder.Build());
        }
    }
}
