using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.Offchain
{
    public enum CommitmentType
    {
        Client = 1,
        Hub = 2
    }

    public interface ICommitment
    {
        Guid CommitmentId { get; }
        Guid ChannelId { get; }
        CommitmentType Type { get; }
        string InitialTransaction { get; }
        string Multisig { get; }
        string AssetId { get; }
        string SignedTransaction { get; }

        string RevokePrivateKey { get; }
        string RevokePubKey { get; }

        decimal ClientAmount { get; }
        decimal HubAmount { get; }

        string LockedAddress { get; }
        string LockedScript { get; }

        DateTime CreateDt { get; }
    }

    public interface ICommitmentRepository
    {
        Task<ICommitment> CreateCommitment(CommitmentType type, Guid channelTransactionId, string multisig, string asset, string revokePrivateKey, string revokePubKey,
            string initialTr, decimal clientAmount, decimal hubAmount, string lockedAddress, string lockedScript);

        Task<ICommitment> GetLastCommitment(string multisig, string asset, CommitmentType type);
        Task SetFullSignedTransaction(Guid commitmentId, string multisig, string asset, string fullSignedCommitment);

        Task UpdateClientPrivateKey(Guid commitmentId, string multisig, string asset, string privateKey);
        Task<IEnumerable<ICommitment>> GetMonitoringCommitments();

        Task CloseCommitmentsOfChannel(string multisig, string asset, Guid channelId);
        Task<ICommitment> GetCommitment(string multisig, string asset, string transactionHex);
    }
}
