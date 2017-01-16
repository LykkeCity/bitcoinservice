using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.Transactions
{
    public interface IBroadcastedTransaction
    {
        string Hash { get; }
    }

    public interface IBroadcastedTransactionRepository
    {
        Task InsertTransaction(string hash);

        Task<IBroadcastedTransaction> GetTransaction(string hash);
    }
}
