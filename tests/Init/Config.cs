using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureRepositories;
using Common;
using Common.Log;
using Core.Bitcoin;
using Core.Enums;
using Core.Settings;
using LkeServices;
using Lykke.AzureQueueIntegration;
using Lykke.SettingsReader;
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
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<AppSettings>("../../../../settings/bitcoinsettings_dev.json");

            var log = new LogToConsole();

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(settings);
            builder.RegisterInstance(log).As<ILog>();
            builder.RegisterInstance(new RpcConnectionParams(settings.BitcoinApi));
            builder.BindAzure(new FakeReloadingManager(settings.BitcoinApi.Db), log);
            builder.BindMongo(settings.BitcoinApi.Db.MongoDataConnString);
            builder.BindCommonServices();

            Services = new AutofacServiceProvider(builder.Build());
        }
    }

    public static class GeneralSettingsReader
    {
        public static T ReadGeneralSettings<T>(string url)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            var settingsData = httpClient.GetStringAsync("").Result;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(settingsData);
        }

        public static T ReadGeneralSettingsLocal<T>(string path)
        {
            var content = File.ReadAllText(path);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
        }
    }

    public class FakeReloadingManager : IReloadingManager<DbSettings>
    {
        private readonly DbSettings _value;

        public FakeReloadingManager(DbSettings value)
        {
            _value = value;
        }

        public Task<DbSettings> Reload() => Task.FromResult(_value);
        public bool WasReloadedFrom(DateTime dateTime)
        {
            return true;
        }

        public bool HasLoaded => true;
        public DbSettings CurrentValue => _value;
    }

    public class AppSettings
    {
        public BaseSettings BitcoinApi { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }
}
