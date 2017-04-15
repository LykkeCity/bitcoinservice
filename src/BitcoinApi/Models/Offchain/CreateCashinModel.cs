using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models.Offchain
{
    public class CreateCashinModel
    {
        public string ClientPubKey { get; set; }

        public string CashinAddress { get; set; }

        public decimal Amount { get; set; }        

        public string Asset { get; set; }        

        public Guid? TransferId { get; set; }
    }
}
