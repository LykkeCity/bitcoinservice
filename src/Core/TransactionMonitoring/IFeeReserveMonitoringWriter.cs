using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.TransactionMonitoring
{
    public interface IFeeReserveMonitoringWriter
    {
        Task AddTransactionFeeReserve(Guid transactionId, List<ICoin> feeCoins);
    }
}
