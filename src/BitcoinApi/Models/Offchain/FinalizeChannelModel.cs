using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models.Offchain
{
    public class FinalizeChannelModel
    {
        public string ClientPubKey { get; set; }        

        public string Asset { get; set; }

        public string ClientRevokePubKey { get; set; }

        public string SignedByClientHubCommitment { get; set; }

        public Guid TransferId { get; set; }

        public Guid? NotifyTxId { get; set; }
    }
}
