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
    public class SpendCommitmentMonitoringWriter : ISpendCommitmentMonitoringWriter
    {
        private IQueueExt _queue;

        public SpendCommitmentMonitoringWriter(Func<string, IQueueExt> queuFactory)
        {
            _queue = queuFactory(Constants.SpendCommitmentOutputQueue);
        }

        public Task AddToMonitoring(Guid commitmentId, string transactionHash)
        {
            return _queue.PutRawMessageAsync(new SpendCommitmentMonitorindMessage()
            {
                PutDateTime = DateTime.UtcNow,
                TransactionHash = transactionHash,
                CommitmentId = commitmentId
            }.ToJson());
        }
    }
}
