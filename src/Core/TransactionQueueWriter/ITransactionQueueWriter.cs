using System;
using System.Threading.Tasks;

namespace Core.TransactionQueueWriter
{
    public interface ITransactionQueueWriter
    {
        Task AddCommand(Guid transactionId, TransactionCommandType type, string command);        
    }
}
