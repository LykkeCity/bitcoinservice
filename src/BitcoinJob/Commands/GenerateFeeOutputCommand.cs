using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackgroundWorker.Commands
{
    public class GenerateFeeOutputCommand
    {
        public string WalletAddress { get; set; }               

        public decimal FeeAmount { get; set; }       

        public uint Count { get; set; }        
    }
}
