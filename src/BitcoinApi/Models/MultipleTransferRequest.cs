using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinApi.Models
{
    public class MultipleTransferRequest
    {
        public Guid? TransactionId { get; set; }

        public string Asset { get; set; }

        public string Destination { get; set; }

        public decimal Fee { get; set; }

        public IEnumerable<ToOneAddress> Sources { get; set; }
    }

    public class ToOneAddress
    {
        public string Address { get; set; }

        public decimal Amount { get; set; }
    }
}
