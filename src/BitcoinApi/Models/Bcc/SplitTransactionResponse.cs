using System;
using System.Collections.Generic;
using System.Text;
using LkeServices.Transactions;

namespace BitcoinApi.Models.Bcc
{
    public class SplitTransactionResponse
    {
        public string Transaction { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }

        public SplitTransactionResponse(BccSplitResult splitResult)
        {
            Transaction = splitResult.Transaction;
            ClientAmount = splitResult.ClientAmount;
            HubAmount = splitResult.HubAmount;
        }
    }
}
