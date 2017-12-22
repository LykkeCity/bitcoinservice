using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin.Protocol.Behaviors;

namespace BitcoinApi.Models
{
    public class CashoutRequest
    {
        public Guid? TransactionId { get; set; }

        public string DestinationAddress { get; set; }

        public string Asset { get; set; }

        public decimal Amount { get; set; }
    }
}
