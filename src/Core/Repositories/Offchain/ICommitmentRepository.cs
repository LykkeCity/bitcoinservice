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
        CommitmentType Type { get; }
        string InitialTransaction { get; }
        string Multisig { get; }
        string AssetName { get; }
        string SignedTransaction { get; }
        string RevokePrivateKey { get; }
        string RevokePubKey { get; }        
    }

    public interface ICommitmentRepository
    {
        Task<ICommitment> CreateCommitment(CommitmentType type, string multisig, string assetName, string revokePrivateKey, string revokePubKey, string initialTr);

        Task<ICommitment> GetLastCommitment(string multisig, string assetName, CommitmentType type);
        Task SetFullSignedTransaction(Guid commitmentId, string multisig, string assetName, string fullSignedCommitment);
    }
}
