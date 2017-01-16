using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Core.Repositories.TransactionSign;

namespace AzureRepositories.TransactionSign
{
    public class SignedTransactionQueue : ISignedTransactionQueue
    {
        private readonly IQueueExt _queue;

        public SignedTransactionQueue(IQueueExt queue)
        {
            _queue = queue;
        }

        public Task AddMessageAsync(string msg)
        {
            return _queue.PutRawMessageAsync(msg);
        }
    }
}
