﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models.Offchain
{
    public class TransferModel
    {
        public string ClientPubKey { get; set; }

        public decimal Amount { get; set; }

        public string Asset { get; set; }
        
        public string ClientPrevPrivateKey { get; set; }

        public bool RequiredOperation { get; set; }

        public Guid? TransferId { get; set; }
    }
}
