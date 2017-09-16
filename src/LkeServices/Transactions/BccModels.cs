using System;
using System.Collections.Generic;
using System.Text;

namespace LkeServices.Transactions
{
    public class BccSplitResult
    {
        public BccTransaction Transaction { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }
    }

    public class BccTransaction
    {
        public string TransactionHex { get; set; }

        public string Outputs { get; set; }
    }

    public class BccOutput
    {
        public long Satoshis { get; set; }

        public string Script { get; set; }
    }
}
