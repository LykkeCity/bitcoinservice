using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models
{
    public class SwapRequest
    {
        public Guid? TransactionId { get; set; }

        public string MultisigCustomer1 { get; set; }

        public decimal Amount1 { get; set; }

        public string Asset1 { get; set; }

        public string MultisigCustomer2 { get; set; }

        public decimal Amount2 { get; set; }

        public string Asset2 { get; set; }
    }
}
