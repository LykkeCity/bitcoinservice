using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.QBitNinja;
using LkeServices.Bitcoin;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using RpcConnectionParams = Core.Settings.RpcConnectionParams;

namespace LkeServices.QBitNinja
{
    public class QBitNinjaApiCaller : IQBitNinjaApiCaller
    {
        private readonly Func<QBitNinjaClient> _clientFactory;
        private readonly RpcConnectionParams _connectionParams;

        public QBitNinjaApiCaller(Func<QBitNinjaClient> clientFactory, RpcConnectionParams connectionParams)
        {
            _clientFactory = clientFactory;
            _connectionParams = connectionParams;
        }

        public async Task<BalanceModel> GetAddressBalance(string walletAddress, bool colored = true, bool unspentonly = true)
        {
            var client = _clientFactory();
            client.Colored = colored;
            return await client.GetBalance(BitcoinAddress.Create(walletAddress, _connectionParams.Network), unspentonly);
        }

        public async Task<BalanceSummary> GetBalanceSummary(string walletAddress)
        {
            var client = _clientFactory();
            client.Colored = true;
            return await client.GetBalanceSummary(BitcoinAddress.Create(walletAddress, _connectionParams.Network));
        }

        public Task<GetTransactionResponse> GetTransaction(string hash)
        {
            var client = _clientFactory();
            client.Colored = true;            
            return client.GetTransaction(uint256.Parse(hash));
        }

        public Task<GetBlockResponse> GetBlock(int blockHeight)
        {
            var client = _clientFactory();
            
            return client.GetBlock(new BlockFeature(blockHeight));
        }
    }
}
