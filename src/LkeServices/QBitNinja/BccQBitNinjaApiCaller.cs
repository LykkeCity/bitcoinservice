using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Core;
using Core.Bitcoin;
using Core.QBitNinja;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace LkeServices.QBitNinja
{
    public class BccQBitNinjaApiCaller : IBccQbBitNinjaApiCaller
    {
        private readonly Func<QBitNinjaClient> _clientFactory;
        private readonly RpcConnectionParams _connectionParams;

        public BccQBitNinjaApiCaller(Func<QBitNinjaClient> clientFactory, [KeyFilter(Constants.BccKey)] RpcConnectionParams connectionParams)
        {
            _clientFactory = clientFactory;
            _connectionParams = connectionParams;
        }

        public async Task<BalanceModel> GetAddressBalance(string walletAddress)
        {
            var client = _clientFactory();
            client.Colored = true;
            return await client.GetBalanceBetween(new BalanceSelector(BitcoinAddress.Create(walletAddress, _connectionParams.Network)), from: BlockFeature.Parse(Constants.BccBlock.ToString()), unspentOnly: true);
        }
    }
}
