using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Helpers;
using Core.QBitNinja;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Wallets;
using Core.Settings;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;

namespace LkeServices.Bitcoin
{
    public class BitcoinOutputsService : IBitcoinOutputsService
    {
        private readonly IQBitNinjaApiCaller _qBitNinjaApiCaller;
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly ISpentOutputRepository _spentOutputRepository;
        private readonly IWalletAddressRepository _walletAddressRepository;
        private readonly RpcConnectionParams _connectionParams;

        public BitcoinOutputsService(IQBitNinjaApiCaller qBitNinjaApiCaller,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            ISpentOutputRepository spentOutputRepository, IWalletAddressRepository walletAddressRepository, RpcConnectionParams connectionParams)
        {
            _qBitNinjaApiCaller = qBitNinjaApiCaller;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _spentOutputRepository = spentOutputRepository;
            _walletAddressRepository = walletAddressRepository;
            _connectionParams = connectionParams;
        }

        public async Task<IEnumerable<ICoin>> GetUnspentOutputs(string walletAddress)
        {
            var outputResponse = await _qBitNinjaApiCaller.GetAddressBalance(walletAddress);
            var coins = outputResponse.Operations.SelectMany(o => o.ReceivedCoins).ToList();

            //get unique saved coins
            var internalSavedOutputs = (await _broadcastedOutputRepository.GetOutputs(walletAddress))
                .Where(o => !coins.Any(c => c.Outpoint.Hash.ToString() == o.TransactionHash && c.Outpoint.N == o.N));

            coins.AddRange(internalSavedOutputs.Select(o =>
                {
                    var coin = new Coin(new OutPoint(uint256.Parse(o.TransactionHash), o.N), new TxOut(new Money(o.Amount, MoneyUnit.Satoshi), o.ScriptPubKey.ToScript()));
                    if (o.AssetId != null)
                        return (ICoin)coin.ToColoredCoin(new BitcoinAssetId(o.AssetId, _connectionParams.Network).AssetId, (ulong)o.Quantity);
                    return coin;
                }));

            var unspentOutputs = await _spentOutputRepository.GetUnspentOutputs(coins.Select(o => new Output(o.Outpoint)));

            coins = coins.Where(o => unspentOutputs.Any(un => un.N == o.Outpoint.N && un.TransactionHash == o.Outpoint.Hash.ToString())).ToList();
            var address = BitcoinAddress.Create(walletAddress);
            switch (address.Type)
            {
                case Base58Type.PUBKEY_ADDRESS:
                    return coins;
                case Base58Type.SCRIPT_ADDRESS:
                    var redeem = await _walletAddressRepository.GetRedeemScript(walletAddress);
                    return coins.OfType<Coin>().Select(x => new ScriptCoin(x, new Script(redeem)))
                           .Concat(
                                coins.OfType<ColoredCoin>().Select(x => new ScriptCoin(x, new Script(redeem)).ToColoredCoin(x.Amount))
                                .Cast<ICoin>());
                default:
                    throw new NotImplementedException();
            }
        }

        public async Task<IEnumerable<ICoin>> GetUncoloredUnspentOutputs(string walletAddress)
        {
            return (await GetUnspentOutputs(walletAddress)).OfType<Coin>();
        }

        public async Task<IEnumerable<ColoredCoin>> GetColoredUnspentOutputs(string walletAddress, AssetId assetIdObj)
        {
            return (await GetUnspentOutputs(walletAddress)).OfType<ColoredCoin>().Where(o => o.AssetId == assetIdObj);
        }

        public async Task<IEnumerable<ColoredCoin>> GetColoredUnspentOutputs(string walletAddress)
        {
            return (await GetUnspentOutputs(walletAddress)).OfType<ColoredCoin>().ToList();
        }
    }
}
