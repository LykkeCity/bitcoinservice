using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models.Offchain
{
    public class BroadcastCommitmentModel
    {
        public string ClientPubKey { get; set; }

        public string Asset { get; set; }

        public string Transaction { get; set; }
    }
}
