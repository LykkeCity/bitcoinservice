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
    public class ReturnBroadcastedOutputsMessageWriter : IReturnBroadcastedOutputsMessageWriter
    {        
        private IQueueExt _queue;

        public ReturnBroadcastedOutputsMessageWriter(Func<string,IQueueExt> queueFactory)
        {
            _queue = queueFactory(Constants.ReturnBroadcatedOutputsQueue);
        }

        public Task AddToReturn(string transactionHex, string address)
        {
            
            return _queue.PutRawMessageAsync(new ReturnBroadcastedOutputMessage
            {
                TransactionHex = transactionHex,
                Address = address
            }.ToJson());
        }
    }
}
