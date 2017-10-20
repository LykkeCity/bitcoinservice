using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Core;
using Core.Repositories.PaidFees;

namespace AzureRepositories.PaidFees
{
    public class PaidFeesTaskWriter : IPaidFeesTaskWriter
    {
        private readonly IQueueExt _queue;

        public PaidFeesTaskWriter(Func<string, IQueueExt> queuFactory)
        {
            _queue = queuFactory(Constants.PaidFeesTasksQueue);
        }

     
        public Task AddTask(string hash, DateTime date, string asset, string multisig)
        {
            return _queue.PutRawMessageAsync(new PaidFeesTask
            {
                TransactionHash = hash,
                Date = date,
                Multisig = multisig,
                Asset = asset
            }.ToJson());
        }
    }
}
