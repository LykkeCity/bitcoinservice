using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.TransactionMonitoring;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.TransactionMonitoring
{
    public class FailedTransactionEntity : TableEntity, IFailedTransaction
    {
        public string TransactionHash { get; set; }

        public string TransactionId { get; set; }

        public DateTime DateTime { get; set; }

        public string Error { get; set; }

        public static FailedTransactionEntity Create(Guid transactionId, string transactionHash, string error)
        {
            return new FailedTransactionEntity
            {
                PartitionKey = "FailedTransaction",
                RowKey = transactionId.ToString(),
                TransactionId = transactionId.ToString(),
                TransactionHash = transactionHash,
                DateTime = DateTime.UtcNow,
                Error = error
            };
        }
    }


    public class FailedTransactionRepository : IFailedTransactionRepository
    {
        private readonly INoSQLTableStorage<FailedTransactionEntity> _table;

        public FailedTransactionRepository(INoSQLTableStorage<FailedTransactionEntity> table)
        {
            _table = table;
        }

        public Task AddFailedTransaction(Guid transactionId, string transactionHash, string error)
        {
            return _table.InsertOrReplaceAsync(FailedTransactionEntity.Create(transactionId, transactionHash, error));
        }

        public async Task<IEnumerable<IFailedTransaction>> GetAllAsync()
        {
            return await _table.GetDataAsync();
        }
    }
}
