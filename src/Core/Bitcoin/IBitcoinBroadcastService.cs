﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Performance;
using NBitcoin;

namespace Core.Bitcoin
{
    public interface IBitcoinBroadcastService
    {
        Task BroadcastTransaction(Guid transactionId, Transaction tx, IPerformanceMonitor monitor = null, bool useHandlers = true, Guid? notifyTxId = null);

        Task BroadcastTransaction(Guid transactionId, List<Guid> notificationIds, Transaction tx, IPerformanceMonitor monitor = null,
            bool useHandlers = true);
    }
}
