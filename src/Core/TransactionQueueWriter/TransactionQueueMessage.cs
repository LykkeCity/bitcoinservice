using System;

namespace Core.TransactionQueueWriter
{
    public class TransactionQueueMessage
    {
        public Guid TransactionId { get; set; }

        public TransactionCommandType Type { get; set; }

        public string Command { get; set; }        

        public int DequeueCount { get; set; }
        
        public string LastError { get; set; }
    }
}
