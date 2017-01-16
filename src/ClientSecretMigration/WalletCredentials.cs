using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ClientSecretMigration
{
    public class WalletCredentialsEntity : TableEntity
    {
        public static string GeneratePartitionKey()
        {
            return "Wallet";
        }

        public string PrivateKey { get; set; }
    }

    public interface IWalletCredentialsRepository
    {
        Task<IEnumerable<WalletCredentialsEntity>> GetDataAsync();
    }

    public class WalletCredentialsRepository : IWalletCredentialsRepository
    {
        private readonly INoSQLTableStorage<WalletCredentialsEntity> _tableStorage;

        public WalletCredentialsRepository(INoSQLTableStorage<WalletCredentialsEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<WalletCredentialsEntity>> GetDataAsync()
        {
            return await _tableStorage.GetDataAsync(WalletCredentialsEntity.GeneratePartitionKey());
        }
    }
}
