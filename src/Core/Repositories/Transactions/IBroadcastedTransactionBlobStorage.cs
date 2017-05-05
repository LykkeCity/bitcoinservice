using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.Transactions
{
    public interface IBroadcastedTransactionBlobStorage
    {
        Task SaveToBlob(Guid transactionId, string hex);

        Task<bool> IsBroadcasted(Guid transactionId);
    }
}
