using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Microsoft.Extensions.Configuration;

namespace PoisonMessagesReenqueue
{
    public class ReenqueueJob
    {
        private readonly IQueueExt _fromQueue;
        private readonly IQueueExt _toQueue;

        public ReenqueueJob(Func<string, IQueueExt> queueFactory, IConfiguration configuration)
        {
            var queueName = configuration.GetValue<string>("QueueName");
            _fromQueue = queueFactory(queueName + "-poison");
            _toQueue = queueFactory(queueName);
        }

        public async Task Start()
        {
            int? cnt = 0;
            int i = 0;
            while ((cnt = await _fromQueue.Count()) > 0)
            {
                var msg = await _fromQueue.GetRawMessageAsync();
                await _toQueue.PutRawMessageAsync(msg.AsString);
                await _fromQueue.FinishRawMessageAsync(msg);
                Console.WriteLine($"Processed {++i} from {cnt}");
            }
        }
    }
}
