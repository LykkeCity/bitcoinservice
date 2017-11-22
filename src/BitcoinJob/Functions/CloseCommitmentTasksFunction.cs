using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Repositories.Offchain;
using Lykke.JobTriggers.Triggers.Attributes;

namespace BitcoinJob.Functions
{
    public class CloseCommitmentTasksFunction
    {
        private readonly ICommitmentRepository _commitmentRepository;

        public CloseCommitmentTasksFunction(ICommitmentRepository commitmentRepository)
        {
            _commitmentRepository = commitmentRepository;
        }

        [QueueTrigger(Constants.CommitmentClosingTaskQueue)]
        public async Task CloseCommitment(CommitmentClosingTask task)
        {
            await _commitmentRepository.CloseCommitmentsOfChannel(task.ChannelId);
        }

    }
}
