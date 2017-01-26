using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public Guid TransactionId { get; set; }

        public static BroadcastedTransactionEntity Create(string hash, Guid transactionId)
        {
            return new BroadcastedTransactionEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = hash,
                TransactionId = transactionId
            };
        }
    }

    public class BroadcastedTransactionRepository : IBroadcastedTransactionRepository
    {
        private const string BlobContainer = "broadcasted-transactions";

        private readonly INoSQLTableStorage<BroadcastedTransactionEntity> _storage;
        private readonly IBlobStorage _blobStorage;

        public BroadcastedTransactionRepository(INoSQLTableStorage<BroadcastedTransactionEntity> storage, IBlobStorage blobStorage)
        {
            _storage = storage;
            _blobStorage = blobStorage;
        }

        public async Task InsertTransaction(string hash, Guid transactionId)
        {
            await _storage.InsertAsync(BroadcastedTransactionEntity.Create(hash, transactionId));
        }

        public async Task<IBroadcastedTransaction> GetTransaction(string hash)
        {
            return await _storage.GetDataAsync(BroadcastedTransactionEntity.GeneratePartitionKey(), hash);
        }

        public async Task SaveToBlob(Guid transactionId, string hex)
        {
            await _blobStorage.SaveBlobAsync(BlobContainer, GetBlobKey(transactionId), Encoding.UTF8.GetBytes(hex));
        }

        public Task<bool> IsBroadcasted(Guid transactionId)
        {
            return _blobStorage.HasBlobAsync(BlobContainer, GetBlobKey(transactionId));
        }
        
        private string GetBlobKey(Guid transactionId)
        {
            return transactionId + ".txt";
        }
    }
}
