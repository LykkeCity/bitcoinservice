using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models.Offchain
{
    public class CreateHubCommitmentModel
    {
        public string ClientPubKey { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }

        public string Asset { get; set; }

        public string SignedByClientChannel { get; set; }
    }
}
