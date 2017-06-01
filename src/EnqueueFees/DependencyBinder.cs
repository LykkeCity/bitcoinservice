using System;
using Autofac;
using AzureRepositories.Assets;
using AzureRepositories.TransactionOutputs;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Core;
using Core.Bitcoin;
using Core.Enums;
using Core.QBitNinja;
using Core.Repositories.Assets;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Wallets;
using Core.Settings;
using LkeServices.Bitcoin;
using LkeServices.QBitNinja;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoRepositories.Mongo;
using MongoRepositories.TransactionOutputs;
using MongoRepositories.Walelts;
using NBitcoin;
using QBitNinja.Client;
using BroadcastedOutputEntity = MongoRepositories.TransactionOutputs.BroadcastedOutputEntity;
using BroadcastedOutputRepository = MongoRepositories.TransactionOutputs.BroadcastedOutputRepository;
using OutputEntity = MongoRepositories.TransactionOutputs.OutputEntity;
using SpentOutputRepository = MongoRepositories.TransactionOutputs.SpentOutputRepository;

namespace EnqueueFees
{
    public class DependencyBinder
    {
        public static IServiceProvider BindAndBuild(IConfigurationRoot configuration)
        {

            var mongoClient = new MongoClient(configuration.GetConnectionString("mongo"));
            var collection = new ServiceCollection();

            collection.AddTransient<EnqueueFeesJob>();
            collection.AddSingleton<Func<string, IQueueExt>>(x => new AzureQueueExt(configuration.GetConnectionString("default"), x));
            collection.AddSingleton<IPregeneratedOutputsQueueFactory, PregeneratedOutputsQueueFactory>();


            collection.AddSingleton<IAssetRepository>(new AssetRepository(new AzureTableStorage<AssetEntity>(configuration.GetConnectionString("dicts"), "Dictionaries", null)));

            collection.AddSingleton<ISpentOutputRepository>(
                new SpentOutputRepository(new MongoStorage<OutputEntity>(mongoClient, "SpentOutputs")));               
            collection.AddSingleton<IWalletAddressRepository>(
                new WalletAddressRepository(new MongoStorage<WalletAddressEntity>(mongoClient, "Wallets")));

            collection.AddSingleton<IBroadcastedOutputRepository>(
                new BroadcastedOutputRepository(new MongoStorage<BroadcastedOutputEntity>(mongoClient, "BroadcastedOutputs")));

            collection.AddSingleton<IInternalSpentOutputRepository>(
                new InternalSpentOutputRepository(new MongoStorage<InternalSpentOutput>(mongoClient, "InternalSpentOutputs")));                
            var network = (NetworkType)configuration.GetValue<int>("BitcoinConfig:Network");

            var rpcConnectionParams = new RpcConnectionParams(new BaseSettings() { NetworkType = network });
            collection.AddSingleton(rpcConnectionParams);

            collection.AddSingleton<Func<QBitNinjaClient>>(x =>
            {               
                return () => new QBitNinjaClient(configuration.GetValue<string>("BitcoinConfig:QBitNinjaBaseUrl"), rpcConnectionParams.Network);
            });
            collection.AddSingleton<IConfiguration>(configuration);
            collection.AddTransient<IQBitNinjaApiCaller, QBitNinjaApiCaller>();
            collection.AddTransient<IBitcoinOutputsService, BitcoinOutputsService>();                  
            return collection.BuildServiceProvider();
        }
    }
}
