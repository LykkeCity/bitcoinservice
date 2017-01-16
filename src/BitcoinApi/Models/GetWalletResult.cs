using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinApi.Models
{
    public class GetWalletResult
    {
        public string MultiSigAddress { get; set; }
        public string ColoredMultiSigAddress { get; set; }
    }
}
