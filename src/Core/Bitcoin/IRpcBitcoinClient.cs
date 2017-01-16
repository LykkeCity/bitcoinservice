using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.Bitcoin
{
    public interface IRpcBitcoinClient
    {
        Task BroadcastTransaction(Transaction tr);

        Task BroadcastTransaction(string tr);

        Task<string> GetTransactionHex(string trId);
    }
}
