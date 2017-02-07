using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models.Offchain
{
    public class TransactionHashResponse
    {
        public string TransactionHash { get; set; }

        public TransactionHashResponse(string transactionHash)
        {
            TransactionHash = transactionHash;
        }
    }
}
