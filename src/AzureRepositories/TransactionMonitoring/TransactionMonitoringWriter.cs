using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Core;
using Core.TransactionMonitoring;

namespace AzureRepositories.TransactionMonitoring
{
    public class TransactionMonitoringWriter : ITransactionMonitoringWriter
    {
        private readonly IQueueExt _queue;

        public TransactionMonitoringWriter(Func<string, IQueueExt> queueFactory)
        {
            _queue = queueFactory(Constants.BroadcastMonitoringQueue);
        }

        public Task AddToMonitoring(Guid transactionId, string transactionHash)
        {
            return _queue.PutRawMessageAsync(new TransactionMonitoringMessage
            {
                TransactionId = transactionId,
                TransactionHash = transactionHash,
                PutDateTime = DateTime.UtcNow
            }.ToJson());
        }
    }
}
