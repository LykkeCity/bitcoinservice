using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Core;
using Core.TransactionQueueWriter;

namespace AzureRepositories.TransactionQueueHolder
{
    public class TransactionQueueWriter : ITransactionQueueWriter
    {
        private readonly IQueueExt _queue;

        public TransactionQueueWriter(Func<string, IQueueExt> queueFactory)
        {
            _queue = queueFactory(Constants.TransactionCommandQueue);
        }


        public async Task AddCommand(Guid transactionId, TransactionCommandType type, string command)
        {
            var msg = new TransactionQueueMessage
            {
                TransactionId = transactionId,
                Type = type,
                Command = command
            };
            await _queue.PutRawMessageAsync(msg.ToJson());
        }        
    }
}
