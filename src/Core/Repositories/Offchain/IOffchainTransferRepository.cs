using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.Offchain
{
    public interface IOffchainTransfer
    {
        Guid TransferId { get; }

        string Multisig { get; }

        string AssetId { get; }

        bool Completed { get; }

        bool Required { get; }

        bool Closed { get;  }

        DateTime CreateDt { get; }
    }

    public interface IOffchainTransferRepository
    {
        Task<IOffchainTransfer> CreateTransfer(string multisig, string asset, bool required);
        Task<IOffchainTransfer> GetTransfer(string multisig, string asset, Guid transferId);
        Task<IOffchainTransfer> GetLastTransfer(string multisig, string assetId);
        Task CompleteTransfer(string multisig, string asset, Guid transferId);
        Task CloseTransfer(string multisig, string asset, Guid transferId);
    }
}
