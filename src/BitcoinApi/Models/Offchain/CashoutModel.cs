using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models.Offchain
{
    public class CashoutModel
    {
        public string ClientPubKey { get; set; }

        public string CashoutAddress { get; set; }

        public string HotWalletAddress { get; set; }

        public string Asset { get; set; }

        public decimal Amount { get; set; }        
    }
}
