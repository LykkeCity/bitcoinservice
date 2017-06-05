using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinApi.Models.Offchain
{
    public class CreateCashoutFromHubModel
    {
        public string ClientPubKey { get; set; }
        
        public string HotWalletAddress { get; set; }

        public string Asset { get; set; }        
    }
}
