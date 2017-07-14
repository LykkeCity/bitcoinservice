using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinApi.Models
{
    public class TransactionIdAndHashResponse : TransactionIdResponse
    {
        public string Hash { get; set; }
    }
}
