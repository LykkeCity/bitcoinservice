using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.Transactions
{
    public interface IBroadcastedTransaction
    {
        string Hash { get; }
        Guid TransactionId { get; }
    }

    public interface IBroadcastedTransactionRepository
    {
        Task InsertTransaction(string hash, Guid transactionId);

        Task<IBroadcastedTransaction> GetTransaction(string hash);

        Task SaveToBlob(Guid transactionId, string hex);

        Task<bool> IsBroadcasted(Guid transactionId);
    }
}
