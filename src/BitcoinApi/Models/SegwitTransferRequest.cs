using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinApi.Models
{
    public class SegwitTransferRequest
    {
        public Guid? TransactionId { get; set; }

        public string SourceAddress { get; set; }
    }
}
