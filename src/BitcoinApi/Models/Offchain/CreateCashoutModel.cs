using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinApi.Models.Offchain
{
    public class CreateCashoutModel
    {
        public string ClientPubKey { get; set; }                

        public string Asset { get; set; }        
    }
}
