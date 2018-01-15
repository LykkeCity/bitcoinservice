using System;
using System.Collections.Generic;
using System.Text;
using Core.Repositories.MultipleCashouts;
using NBitcoin;

namespace LkeServices.Transactions
{
    public class CreateMultiCashoutTransactionResult
    {
        public List<ICashoutRequest> UsedRequests { get; set; }

        public Transaction Transaction { get; set; }
    }
}
