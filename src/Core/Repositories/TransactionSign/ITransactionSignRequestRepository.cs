using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.TransactionSign
{
    public interface ITransactionSignRequest
    {
        Guid TransactionId { get; }
        bool? Invalidated { get; }
        string RawRequest { get; }

        bool DoNotSign { get; set; }
    }

    public interface ITransactionSignRequestRepository
    {
        Task<ITransactionSignRequest> GetSignRequest(Guid transactionId);

        Task<Guid> InsertTransactionId(Guid? transactionId, string rawRequest);

        Task InvalidateTransactionId(Guid transactionId);
        Task DoNotSign(Guid transactionId);
    }
}
