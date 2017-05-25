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
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<BaseSettings>("../../../../settings/bitcoinsettings.json");

            var log = new LogToConsole();

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(settings);
            builder.RegisterInstance(log).As<ILog>();
            builder.RegisterInstance(new RpcConnectionParams(settings));
            builder.BindAzure(settings, log);
            builder.BindMongo(settings);
            builder.BindCommonServices();

            Services = new AutofacServiceProvider(builder.Build());
        }
    }
}
