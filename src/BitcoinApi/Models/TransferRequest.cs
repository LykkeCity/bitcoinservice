using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models
{
    public class TransferRequest
    {
        public Guid? TransactionId { get; set; }

        public string SourceAddress { get; set; }

        public string DestinationAddress { get; set; }

        public decimal Amount { get; set; }

        public string Asset { get; set; }
    }
}
