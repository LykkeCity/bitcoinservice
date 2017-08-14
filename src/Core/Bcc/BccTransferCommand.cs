using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Bcc
{
    public class BccTransferCommand
    {
        public string SourceAddress { get; set; }

        public string DestinationAddress { get; set; }

        public decimal Amount { get; set; }
    }
}
