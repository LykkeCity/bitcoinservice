using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models
{
    public class TransactionResponse
    {
        public string Transaction { get; set; }
        public Guid TransactionId { get; set; }
        public decimal Fee { get; set; }
    }
}
