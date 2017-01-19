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

        public Task<GetTransactionResponse> GetTransaction(string hash)
        {
            var client = _clientFactory();
            return client.GetTransaction(uint256.Parse(hash));
        }
    }
}
