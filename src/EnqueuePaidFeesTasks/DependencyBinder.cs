using System;
using System.Threading.Tasks;
using AzureRepositories.PaidFees;
using AzureStorage;
using AzureStorage.Blob;
using AzureStorage.Queue;
using Core.Repositories.PaidFees;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoRepositories.Mongo;
using MongoRepositories.PaidFees;
using MongoRepositories.TransactionOutputs;
using MongoRepositories.Transactions;
using MongoRepositories.TransactionSign;

namespace EnqueuePaidFeesTasks
{
    public class DependencyBinder
    {
        public static IServiceProvider BindAndBuild(IConfigurationRoot configuration)
        {
            var collection = new ServiceCollection();

            collection.AddTransient<Job>();

            collection.AddTransient<Func<string, IQueueExt>>(x => queueName => AzureQueueExt.Create(new FakeReloadingManager(configuration.GetConnectionString("azure")), queueName));

            collection.AddSingleton<IBlobStorage>(AzureBlobStorage.Create(new FakeReloadingManager(configuration.GetConnectionString("azure"))));

            var mongoClient = new MongoClient(configuration.GetConnectionString("mongo"));

            collection.AddSingleton<ISpentOutputRepository>(new SpentOutputRepository(new MongoStorage<OutputEntity>(mongoClient, "SpentOutputs")));

            collection.AddSingleton<IBroadcastedOutputRepository>(
                new BroadcastedOutputRepository(new MongoStorage<BroadcastedOutputEntity>(mongoClient, "BroadcastedOutputs")));

            collection.AddSingleton<IBroadcastedTransactionRepository>(
                new BroadcastedTransactionRepository(new MongoStorage<BroadcastedTransactionEntity>(mongoClient, "BroadcastedTransactions")));

            collection.AddSingleton<IPaidFeesTaskWriter, PaidFeesTaskWriter>();

            collection.AddSingleton<IPaidFeesRepository>(
                new PaidFeesRepository(new MongoStorage<PaidFeesEntity>(mongoClient, "PaidFees")));

            collection.AddSingleton<ITransactionSignRequestRepository>(
                new TransactionSignRequestRepository(new MongoStorage<TransactionSignRequestEntity>(mongoClient, "SignRequests")));

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
        public bool HasLoaded => true;
        public string CurrentValue => _value;
    }
}
