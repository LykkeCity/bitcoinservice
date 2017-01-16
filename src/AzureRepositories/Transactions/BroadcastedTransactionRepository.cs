using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Transactions;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Transactions
{
    public class BroadcastedTransactionEntity : TableEntity, IBroadcastedTransaction
    {
        public static string GeneratePartitionKey()
        {
            return "Transaction";
        }

        public string Hash => RowKey;

        public static BroadcastedTransactionEntity Create(string hash)
        {
            return new BroadcastedTransactionEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = hash
            };
        }
    }

    public class BroadcastedTransactionRepository : IBroadcastedTransactionRepository
    {
        private readonly INoSQLTableStorage<BroadcastedTransactionEntity> _storage;

        public BroadcastedTransactionRepository(INoSQLTableStorage<BroadcastedTransactionEntity> storage)
        {
            _storage = storage;
        }

        public async Task InsertTransaction(string hash)
        {
            await _storage.InsertAsync(BroadcastedTransactionEntity.Create(hash));
        }

        public async Task<IBroadcastedTransaction> GetTransaction(string hash)
        {
            return await _storage.GetDataAsync(BroadcastedTransactionEntity.GeneratePartitionKey(), hash);
        }
    }
}
