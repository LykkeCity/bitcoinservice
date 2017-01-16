using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LkeServices.Transactions
{
    public class CreateTransactionResponse
    {
        public string Transaction { get; set; }
        public Guid TransactionId { get; set; }

        public CreateTransactionResponse(string transactionHex, Guid transactionId)
        {
            Transaction = transactionHex;
            TransactionId = transactionId;
        }
    }
}
