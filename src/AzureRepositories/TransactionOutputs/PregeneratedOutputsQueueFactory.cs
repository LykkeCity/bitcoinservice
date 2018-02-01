using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Core;
using Core.Repositories.TransactionOutputs;

namespace AzureRepositories.TransactionOutputs
{
    public class PregeneratedOutputsQueueFactory : IPregeneratedOutputsQueueFactory
    {
        private readonly Func<string, IQueueExt> _queueFactory;

        public PregeneratedOutputsQueueFactory(Func<string, IQueueExt> queueFactory)
        {
            _queueFactory = queueFactory;
        }

        private string CreateQueueName(string assetId)
        {
            return "po-" + assetId.Replace(".", "-").Replace("_", "-").ToLower();
        }

        public IPregeneratedOutputsQueue Create(string assetId)
        {
            var queue = CreateQueueName(assetId);
            return new PregeneratedOutputsQueue(_queueFactory(queue), queue);
        }

        public IPregeneratedOutputsQueue CreateFeeQueue()
        {
            return new PregeneratedOutputsQueue(_queueFactory(Constants.PregeneratedFeePoolQueue), Constants.PregeneratedFeePoolQueue);
        }
    }
}
