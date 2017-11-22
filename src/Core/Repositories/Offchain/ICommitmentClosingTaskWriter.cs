using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.Offchain
{
    public interface ICommitmentClosingTaskWriter
    {
        Task Add(Guid channelId);
    }

    public class CommitmentClosingTask
    {
        public Guid ChannelId { get; set; }
    }
}
