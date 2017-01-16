using System;
using AzureStorage.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClientSecretMigration
{
    public class DependencyBinder
    {
        public static IServiceProvider BindAndBuild(IConfigurationRoot configuration)
        {
            var collection = new ServiceCollection();
            
            collection.AddTransient<MigrationJob>();

            collection.AddSingleton<IWalletCredentialsRepository>(
                new WalletCredentialsRepository(
                    new AzureTableStorage<WalletCredentialsEntity>(configuration.GetConnectionString("wallet"),
                        "WalletCredentials", null)));

            collection.AddSingleton<IKeyRepository>(
                new KeyRepository(
                    new AzureTableStorage<KeyEntity>(configuration.GetConnectionString("keystorage"),
                        "SigningSecrets", null)));

            collection.AddSingleton<IConfiguration>(configuration);

            return collection.BuildServiceProvider();
        }
    }
}
