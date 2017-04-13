using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LkeServices.Transactions;

namespace BitcoinApi.Models.Offchain
{
    public class OffchainApiResponse
    {
        public string Transaction { get; set; }

        public Guid TransferId { get; set; }

        public OffchainApiResponse(OffchainResponse response)
        {
            Transaction = response.TransactionHex;
            TransferId = response.TransferId;
        }


    }
}
