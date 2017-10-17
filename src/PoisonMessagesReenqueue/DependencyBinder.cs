using System;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Core.Repositories.Wallets;
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

            collection.AddTransient<Func<string, IQueueExt>>(x => queueName => new AzureQueueExt(configuration.GetConnectionString("job"), queueName));

            collection.AddSingleton<IConfiguration>(configuration);

            return collection.BuildServiceProvider();
        }
    }
}
