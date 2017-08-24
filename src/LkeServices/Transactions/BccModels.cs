using System;
using System.Collections.Generic;
using System.Text;

namespace LkeServices.Transactions
{
    public class BccSplitResult
    {
        public string Transaction { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }
    }
}
