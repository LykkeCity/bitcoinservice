using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models.Offchain
{
    public class OffchainResponse
    {
        public string Transaction { get; set; }

        public OffchainResponse(string transaction)
        {
            Transaction = transaction;
        }
    }
}
