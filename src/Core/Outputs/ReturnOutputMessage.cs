using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Outputs
{
    public class ReturnOutputMessage
    {
        public string TransactionHex { get; set; }

        public List<string> Addresses { get; set; }
    }
}
