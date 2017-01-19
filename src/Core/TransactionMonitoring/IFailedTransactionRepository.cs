using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.TransactionMonitoring
{
    public interface IFailedTransaction
    {
        string TransactionHash { get; }
    }
    public interface IFailedTransactionRepository
    {
        Task AddFailedTransaction(Guid transactionId, string transactionHash);
    }
}
