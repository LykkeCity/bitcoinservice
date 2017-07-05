using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.Offchain
{
    public interface ICommitmentBroadcast
    {
        Guid CommitmentId { get; set; }
        string TransactionHash { get; set; }
        DateTime Date { get; set; }
        CommitmentBroadcastType Type { get; set; }
        decimal ClientAmount { get; set; }
        decimal HubAmount { get; set; }

        decimal RealClientAmount { get; set; }
        decimal RealHubAmount { get; set; }
        string PenaltyTransactionHash { get; set; }
    }

    public enum CommitmentBroadcastType
    {
        Valid,
        Revoked
    }


    public interface ICommitmentBroadcastRepository
    {
        Task<ICommitmentBroadcast> InsertCommitmentBroadcast(Guid commitmentId, string transactionHash, CommitmentBroadcastType type, decimal clientAmount, decimal hubAmount, decimal realClientAmount, decimal realHubAmount, string penaltyHash);

        Task SetPenaltyTransactionHash(Guid commitmentId, string hash);
        Task<IEnumerable<ICommitmentBroadcast>> GetLastCommitmentBroadcasts(int limit);
    }
}
