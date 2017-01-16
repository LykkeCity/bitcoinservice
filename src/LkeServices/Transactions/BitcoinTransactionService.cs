using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Repositories.Transactions;
using NBitcoin;

namespace LkeServices.Transactions
{
    public interface IBitcoinTransactionService
    {
        Task<Transaction> GetTransaction(string hash);
        Task<string> GetTransactionHex(string hash);
    }

    public class BitcoinTransactionService : IBitcoinTransactionService
    {
        private readonly IRpcBitcoinClient _rpcBitcoinClient;

        public BitcoinTransactionService(IRpcBitcoinClient rpcBitcoinClient)
        {
            _rpcBitcoinClient = rpcBitcoinClient;
        }

        public async Task<Transaction> GetTransaction(string hash)
        {
            return new Transaction(await GetTransactionHex(hash));
        }

        public async Task<string> GetTransactionHex(string hash)
        {
            return await _rpcBitcoinClient.GetTransactionHex(hash);
        }
    }
}
