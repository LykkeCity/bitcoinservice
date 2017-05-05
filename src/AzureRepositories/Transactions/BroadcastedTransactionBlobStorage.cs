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

    public class BroadcastedTransactionBlobStorage : IBroadcastedTransactionBlobStorage
    {
        private const string BlobContainer = "broadcasted-transactions";

        private readonly IBlobStorage _blobStorage;

        public BroadcastedTransactionBlobStorage(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage;
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
