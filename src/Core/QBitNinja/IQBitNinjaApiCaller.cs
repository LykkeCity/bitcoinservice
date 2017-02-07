﻿using System.Threading.Tasks;
using QBitNinja.Client.Models;

namespace Core.QBitNinja
{
    public interface IQBitNinjaApiCaller
    {
        Task<BalanceModel> GetAddressBalance(string walletAddress, bool colored = true, bool unspentonly = true);
        Task<GetTransactionResponse> GetTransaction(string hash);
        Task<GetBlockResponse> GetBlock(int blockHeight);
    }
}
