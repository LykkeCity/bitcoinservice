using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.Transactions
{
    public enum TransactionBlobType
    {
        Initial = 0,        
        Signed = 1,
        Client = 2,
        Prebroadcasted = 3,
    }

    public interface ITransactionBlobStorage
    {
        Task<string> GetTransaction(Guid transactionId, TransactionBlobType type);

        Task AddOrReplaceTransaction(Guid transactionId, TransactionBlobType type, string transactionHex);
    }
}
