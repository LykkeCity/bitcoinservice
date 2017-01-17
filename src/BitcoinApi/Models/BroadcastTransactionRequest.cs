using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models
{
    public class BroadcastTransactionRequest
    {
        public Guid TransactionId { get; set; }

        public string Transaction { get; set; }
    }
}
