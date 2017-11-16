using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Core;
using Core.Repositories.Offchain;

namespace AzureRepositories.Offchain
{
    public class CommitmentClosingTaskWriter : ICommitmentClosingTaskWriter
    {
        private readonly IQueueExt _queue;

        public CommitmentClosingTaskWriter(Func<string, IQueueExt> queueFactory)
        {
            _queue = queueFactory(Constants.CommitmentClosingTaskQueue);
        }

        public Task Add(Guid channelId)
        {
            return _queue.PutRawMessageAsync(new CommitmentClosingTask
            {
                ChannelId = channelId
            }.ToJson());
        }
    }
}
