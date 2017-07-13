using System;
using System.Collections.Generic;
using System.Text;

namespace Core.TransactionQueueWriter.Commands
{
    public class MultipleTransferCommand
    {
        public string Destination { get; set; }

        public string Asset { get; set; }

        public decimal Fee { get; set; }

        public Dictionary<string, decimal> Addresses { get; set; }
    }
}
