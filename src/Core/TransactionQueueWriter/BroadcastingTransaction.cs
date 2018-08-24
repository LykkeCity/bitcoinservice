using System;

namespace Core.TransactionQueueWriter
{
    public class BroadcastingTransaction
    {        
        public Guid TransactionId { get; set; }

        public int DequeueCount { get; set; }

        public string LastError { get; set; }
        public TransactionCommandType TransactionCommandType { get; set; }
    }
}
