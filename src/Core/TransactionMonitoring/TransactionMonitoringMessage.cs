using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.TransactionMonitoring
{
    public class TransactionMonitoringMessage
    {
        public Guid TransactionId { get; set; }

        public string TransactionHash { get; set; }

        public DateTime PutDateTime { get; set; }
        public string LastError { get; set; }
    }
}
