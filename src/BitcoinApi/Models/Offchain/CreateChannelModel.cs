using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models.Offchain
{
    public class CreateChannelModel
    {
        public string ClientPubKey { get; set; }

        public string HotWalletAddress { get; set; }
        
        public decimal HubAmount { get; set; }

        public string Asset { get; set; }

        public bool RequiredOperation { get; set; }

        public Guid? TransferId { get; set; }
        public decimal? ClientAmount { get; set; }
    }
}
