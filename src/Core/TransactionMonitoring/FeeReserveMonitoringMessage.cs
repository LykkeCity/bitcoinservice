using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;

namespace Core.TransactionMonitoring
{
    public class FeeReserveMonitoringMessage
    {
        public Guid TransactionId { get; set; }

        public List<SerializableCoin> FeeCoins { get; set; }

        public DateTime PutDateTime { get; set; }
    }
}
