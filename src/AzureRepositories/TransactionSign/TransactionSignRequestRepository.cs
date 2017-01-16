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
        public string InitialTransaction { get; set; }
        public string SignedTransaction1 { get; set; }
        public string SignedTransaction2 { get; set; }
        public int RequiredSignCount { get; set; }


        public static TransactionSignRequestEntity Create(Guid transactionId, string initialTr, int requiredSignCount)
        {
            return new TransactionSignRequestEntity
            {
                RowKey = transactionId.ToString(),
                PartitionKey = GeneratePartition(),
                InitialTransaction = initialTr,
                RequiredSignCount = requiredSignCount
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

        public Task InsertSignRequest(Guid transactionId, string initialTr, int requiredSignCount)
        {
            return _table.InsertAsync(TransactionSignRequestEntity.Create(transactionId, initialTr, requiredSignCount));
        }

        public async Task<ITransactionSignRequest> SetSignedTransaction(Guid transactionId, string signedTr)
        {
            return await _table.ReplaceAsync(TransactionSignRequestEntity.GeneratePartition(), transactionId.ToString(), entity =>
            {
                if (entity.SignedTransaction1 == null || entity.RequiredSignCount == 1)
                    entity.SignedTransaction1 = signedTr;
                else
                    entity.SignedTransaction2 = signedTr;

                return entity;
            });
        }
    }
}
