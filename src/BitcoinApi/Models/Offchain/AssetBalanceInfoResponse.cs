using System;
using System.Collections.Generic;
using System.Text;
using LkeServices.Transactions;

namespace BitcoinApi.Models.Offchain
{
    public class AssetBalanceInfoResponse
    {
        public List<MultisigBalanceInfo> Balances { get; set; }


        public AssetBalanceInfoResponse(AssetBalanceInfo info)
        {
            Balances = info.Balances;
        }
    }
}
