using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using AzureRepositories.TransactionOutputs;
using AzureRepositories.Walelts;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Core.Bitcoin;
using Core.Enums;
using Core.QBitNinja;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Wallets;
using Core.Settings;
using LkeServices.QBitNinja;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;
using MongoRepositories.Mongo;
using QBitNinja.Client;

namespace MigrationScript
{
    public class DependencyBinder
    {
        public static IServiceProvider BindAndBuild(IConfigurationRoot configuration)
        {
            var collection = new ServiceCollection();

            collection.AddTransient<MigrationJob>();
            var mongoClient = new MongoClient(configuration.GetConnectionString("mongo"));
            collection.AddSingleton<Func<string, IWalletAddressRepository>>(
                conn =>
                {
                    switch (conn)
                    {
                        case "azure":
                            return new WalletAddressRepository(
                                new AzureTableStorage<WalletAddressEntity>(configuration.GetConnectionString(conn),
                                    "Wallets", null));
                        case "mongo":
                            return new MongoRepositories.Walelts.WalletAddressRepository(new MongoStorage<MongoRepositories.Walelts.WalletAddressEntity>(mongoClient,
                                "Wallets"));
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });

            collection.AddSingleton<IConfiguration>(configuration);

            return collection.BuildServiceProvider();
        }
    }
}
