using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Core.Repositories.Wallets;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin.BuilderExtensions;

namespace PoisonMessagesReenqueue
{
    public class DependencyBinder
    {
        public static IServiceProvider BindAndBuild(IConfigurationRoot configuration)
        {
            var collection = new ServiceCollection();

            collection.AddTransient<ReenqueueJob>();

            collection.AddTransient<Func<string, IQueueExt>>(x => queueName => AzureQueueExt.Create(new FakeReloadingManager(configuration.GetConnectionString("job")), queueName));

            collection.AddSingleton<IConfiguration>(configuration);

            return collection.BuildServiceProvider();
        }
    }

    public class FakeReloadingManager : IReloadingManager<string>
    {
        private readonly string _value;

        public FakeReloadingManager(string value)
        {
            _value = value;
        }

        public Task<string> Reload() => Task.FromResult(_value);
        public bool WasReloadedFrom(DateTime dateTime)
        {
            return true;
        }

        public bool HasLoaded => true;
        public string CurrentValue => _value;
    }
}
