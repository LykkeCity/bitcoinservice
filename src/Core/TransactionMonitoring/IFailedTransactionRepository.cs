using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.TransactionMonitoring
{
    public interface IFailedTransaction
    {
        string TransactionId { get; }
        string TransactionHash { get; }
        DateTime DateTime { get; }
        string Error { get; }
    }

    public interface IFailedTransactionRepository
    {
        Task AddFailedTransaction(Guid transactionId, string transactionHash, string error);
        Task<IEnumerable<IFailedTransaction>> GetAllAsync();
    }
}
