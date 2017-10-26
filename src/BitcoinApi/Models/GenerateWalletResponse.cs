using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinApi.Models
{
    public class GenerateWalletResponse
    {
        public string Address { get; set; }
        public string PubKey { get; set; }
        public string Tag { get; set; }
    }
}
