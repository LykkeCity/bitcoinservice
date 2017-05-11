using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Perfomance;
using NBitcoin;

namespace Core.Bitcoin
{
    public interface IBitcoinBroadcastService
    {
        Task BroadcastTransaction(Guid transactionId, Transaction tx, IPerfomanceMonitor monitor = null, bool useHandlers = true);
    }
}
