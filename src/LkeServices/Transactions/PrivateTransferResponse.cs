using System;
using System.Collections.Generic;
using System.Text;

namespace LkeServices.Transactions
{
    public class PrivateTransferResponse : CreateTransactionResponse
    {
        public decimal Fee { get; set; }


        public PrivateTransferResponse(string transactionHex, Guid transactionId, decimal fee) : base(transactionHex, transactionId)
        {
            Fee = fee;
        }

    }
}
