using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackgroundWorker
{
    public class BroadcastingTransaction
    {        
        public Guid TransactionId { get; set; }

        public int DequeueCount { get; set; }

        public string LastError { get; set; }
    }
}
