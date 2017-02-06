using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models
{
    public class RetryFailedRequest
    {
        public Guid TransactionId { get; set; }
    }
}
