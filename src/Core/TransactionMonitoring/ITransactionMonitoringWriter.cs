using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.TransactionMonitoring
{
    public  interface ITransactionMonitoringWriter
    {
        Task AddToMonitoring(Guid transactionId, string transactionHash);
    }
}
