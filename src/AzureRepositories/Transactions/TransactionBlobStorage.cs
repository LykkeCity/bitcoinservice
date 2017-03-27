using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Transactions;

namespace AzureRepositories.Transactions
{
    public class TransactionBlobStorage : ITransactionBlobStorage
    {
        private const string BlobContainer = "transactions";

        private readonly IBlobStorage _blobStorage;

        public TransactionBlobStorage(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage;
        }

        public async Task<string> GetTransaction(Guid transactionId, TransactionBlobType type)
        {
            var key = GenerateKey(transactionId, type);
            if (await _blobStorage.HasBlobAsync(BlobContainer, key))
                return await _blobStorage.GetAsTextAsync(BlobContainer, key);
            return null;
        }

        public async Task AddOrReplaceTransaction(Guid transactionId, TransactionBlobType type, string transactionHex)
        {
            var key = GenerateKey(transactionId, type);
            if (await _blobStorage.HasBlobAsync(BlobContainer, key))
                await _blobStorage.DelBlobAsync(BlobContainer, key);
            await _blobStorage.SaveBlobAsync(BlobContainer, key, Encoding.UTF8.GetBytes(transactionHex));
        }

        private string GenerateKey(Guid transactionId, TransactionBlobType type)
        {
            return $"{transactionId}.{type}.txt";
        }
    }
}
