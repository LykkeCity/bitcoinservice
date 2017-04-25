using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LkeServices.Transactions;

namespace BitcoinApi.Models.Offchain
{
    public class OffchainBalanceResponse
    {
        public Dictionary<string, OffchainBalancePair> Channels { get; set; }

        public OffchainBalanceResponse(OffchainBalance balance)
        {
            Channels = balance.Channels;
        }
    }
}
