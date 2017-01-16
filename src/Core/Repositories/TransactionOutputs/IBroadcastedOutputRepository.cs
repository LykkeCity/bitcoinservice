using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.TransactionOutputs
{
    public interface IBroadcastedOutput : IOutput
    {
        Guid TransactionId { get; }

        string Address { get; }

        string ScriptPubKey { get; }

        string AssetId { get; }

        long Amount { get; }

        long Quantity { get; }
    }

    public interface IBroadcastedOutputRepository
    {
        Task InsertOutputs(IEnumerable<IBroadcastedOutput> outputs);
        Task<IEnumerable<IBroadcastedOutput>> GetOutputs(string address);
        Task SetTransactionHash(Guid transactionId, string transactionHash);
        Task DeleteOutput(string transactionHash, int n);
    }
}
