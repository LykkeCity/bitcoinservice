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
        DateTime Date { get; }
    }

    public interface IBroadcastedTransactionRepository
    {
        Task InsertTransaction(string hash, Guid transactionId);

        Task<IBroadcastedTransaction> GetTransaction(string hash);

        Task<IBroadcastedTransaction> GetTransactionById(Guid id);

        Task<IEnumerable<IBroadcastedTransaction>> GetTrasactions(DateTime startDt, DateTime endDt);
    }
}
