using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureRepositories;
using Common;
using Common.Log;
using Core.Bitcoin;
using Core.Enums;
using Core.Settings;
using LkeServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoRepositories;

// ReSharper disable once CheckNamespace
namespace Bitcoin.Tests
{
    [TestClass]
    public class Config
    {
        public static IServiceProvider Services { get; set; }
        public static ILog Logger => Services.GetService<ILog>();

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<GeneralSettings>("../../../../settings/bitcoinsettings_dev.json");

            var log = new LogToConsole();

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(settings);
            builder.RegisterInstance(log).As<ILog>();
            builder.RegisterInstance(new RpcConnectionParams(settings.BitcoinApi));
            builder.BindAzure(settings.BitcoinApi, settings.SlackNotifications, log);
            builder.BindMongo(settings.BitcoinApi);
            builder.BindCommonServices();

            Services = new AutofacServiceProvider(builder.Build());
        }
    }
}
