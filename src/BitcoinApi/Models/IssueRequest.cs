using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models
{
    public class IssueRequest
    {
        public Guid? TransactionId { get; set; }
        public string Address { get; set; }
        public string Asset { get; set; }
        public decimal Amount { get; set; }
    }
}
