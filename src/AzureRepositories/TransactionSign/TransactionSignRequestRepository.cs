using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.TransactionSign;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.TransactionSign
{
    public class TransactionSignRequestEntity : TableEntity, ITransactionSignRequest
    {
        public Guid TransactionId => Guid.Parse(RowKey);
        public bool? Invalidated { get; set; }
        public string RawRequest { get; set; }


        public static TransactionSignRequestEntity Create(Guid transactionId, string rawRequest)
        {
            return new TransactionSignRequestEntity
            {
                RowKey = transactionId.ToString(),
                PartitionKey = GeneratePartition(),
                RawRequest = rawRequest
            };
        }

        public static string GeneratePartition()
        {
            return "TransactionSignRequest";
        }
    }

    public class TransactionSignRequestRepository : ITransactionSignRequestRepository
    {
        private readonly INoSQLTableStorage<TransactionSignRequestEntity> _table;

        public TransactionSignRequestRepository(INoSQLTableStorage<TransactionSignRequestEntity> table)
        {
            _table = table;
        }

        public async Task<ITransactionSignRequest> GetSignRequest(Guid transactionId)
        {
            return await _table.GetDataAsync(TransactionSignRequestEntity.GeneratePartition(), transactionId.ToString());
        }

        public async Task<Guid> InsertTransactionId(Guid? transactionId, string rawRequest)
        {
            var guid = transactionId ?? Guid.NewGuid();
            await _table.InsertOrReplaceAsync(TransactionSignRequestEntity.Create(guid, rawRequest));
            return guid;
        }

        public async Task InvalidateTransactionId(Guid transactionId)
        {
            await _table.ReplaceAsync(TransactionSignRequestEntity.GeneratePartition(), transactionId.ToString(),
                entity =>
                {
                    entity.Invalidated = true;
                    return entity;
                });
        }
    }
}
