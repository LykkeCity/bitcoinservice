using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Core;
using Core.Outputs;

namespace AzureRepositories.TransactionOutputs
{
    public class ReturnOutputsMessageWriter : IReturnOutputsMessageWriter
    {        
        private IQueueExt _queue;

        public ReturnOutputsMessageWriter(Func<string,IQueueExt> queueFactory)
        {
            _queue = queueFactory(Constants.ReturnBroadcatedOutputsQueue);
        }

        public Task AddToReturn(string transactionHex, List<string> address)
        {
            
            return _queue.PutRawMessageAsync(new ReturnOutputMessage
            {
                TransactionHex = transactionHex,
                Addresses = address
            }.ToJson());
        }
    }
}
