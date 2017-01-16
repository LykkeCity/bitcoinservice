using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.TransactionSign
{
    public interface ITransactionSignRequest
    {
        Guid TransactionId { get;}

        string InitialTransaction { get; }

        string SignedTransaction1 { get; }

        string SignedTransaction2 { get; }

        int RequiredSignCount { get; }        
    }

    public interface ITransactionSignRequestRepository
    {
        Task<ITransactionSignRequest> GetSignRequest(Guid transactionId);

        Task InsertSignRequest(Guid transactionId, string initialTr, int requiredSignCount);

        Task<ITransactionSignRequest> SetSignedTransaction(Guid transactionId, string signedTr);
    }
}
