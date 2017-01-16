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
using MigrationScript.Models;
using Microsoft.EntityFrameworkCore;
using QBitNinja.Client;

namespace MigrationScript
{
    public class DependencyBinder
    {
        public static IServiceProvider BindAndBuild(IConfigurationRoot configuration)
        {
            var collection = new ServiceCollection();

            collection.AddDbContext<BitcoinContext>((o) =>
            {
                o.UseSqlServer(configuration.GetConnectionString("default"));
            });

            collection.AddTransient<MigrationJob>();

            collection.AddSingleton<IWalletAddressRepository>(
                new WalletAddressRepository(
                    new AzureTableStorage<WalletAddressEntity>(configuration.GetConnectionString("job"),
                        "Wallets", null)));

            collection.AddSingleton<IKeyRepository>(
                new KeyRepository(
                    new AzureTableStorage<KeyEntity>(configuration.GetConnectionString("keystorage"),
                        "SigningSecrets", null)));

            collection.AddSingleton<IConfiguration>(configuration);

            return collection.BuildServiceProvider();
        }
    }
}
