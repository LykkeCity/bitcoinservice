using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models.Offchain
{
    public class CloseChannelModel
    {
        public string ClientPubKey { get; set; }

        public string CashoutAddress { get; set; }

        public string HotWalletPubKey { get; set; }

        public string Asset { get; set; }

    }
}
