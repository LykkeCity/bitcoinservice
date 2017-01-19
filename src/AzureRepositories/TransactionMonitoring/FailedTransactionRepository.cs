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

        public Guid TransactionId => Guid.Parse(RowKey);

        public static FailedTransactionEntity Create(Guid transactionId, string transactionHash)
        {
            return new FailedTransactionEntity
            {
                PartitionKey = "FailedTransaction",
                RowKey = transactionId.ToString(),
                TransactionHash = transactionHash
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

        public Task AddFailedTransaction(Guid transactionId, string transactionHash)
        {
            return _table.InsertAsync(FailedTransactionEntity.Create(transactionId, transactionHash));
        }
    }
}
