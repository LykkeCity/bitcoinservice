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
            ISpentOutputRepository spentOutputRepository, IWalletAddressRepository walletAddressRepository,
            RpcConnectionParams connectionParams)
        {
            _qBitNinjaApiCaller = qBitNinjaApiCaller;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _spentOutputRepository = spentOutputRepository;
            _walletAddressRepository = walletAddressRepository;
            _connectionParams = connectionParams;
        }


        ////temp method for workaround ninja bug
        //private async Task<List<ICoin>> GetNinjaCoins(string walletAddress, int confirmationsCount)
        //{
        //    var coinsArray = new IEnumerable<ICoin>[_settings.RepeatNinjaCount];
        //    var coinsMap = new HashSet<OutPoint>[_settings.RepeatNinjaCount];

        //    for (var i = 0; i < _settings.RepeatNinjaCount; i++)
        //    {
        //        var outputResponse = await _qBitNinjaApiCaller.GetAddressBalance(walletAddress);
        //        var coins = outputResponse.Operations
        //                                    .Where(x => x.Confirmations >= Math.Max(1, confirmationsCount))
        //                                    .SelectMany(o => o.ReceivedCoins).ToList();
        //        coinsArray[i] = coins;
        //        coinsMap[i] = new HashSet<OutPoint>(coins.Select(o => o.Outpoint));
        //        await Task.Delay(100);
        //    }

        //    var intersect = coinsMap[0];
        //    for (var i = 1; i < _settings.RepeatNinjaCount; i++)
        //        intersect.IntersectWith(coinsMap[i]);

        //    var result = coinsArray[0].Where(o => intersect.Contains(o.Outpoint)).ToList();
        //    await _ninjaBlobStorage.Save(walletAddress, result);
        //    return result;
        //}


        public async Task<IEnumerable<ICoin>> GetUnspentOutputs(string walletAddress, int confirmationsCount = 0)
        {
            var outputResponse = await _qBitNinjaApiCaller.GetAddressBalance(walletAddress);
            var coins = outputResponse.Operations
                                        .Where(x => x.Confirmations >= Math.Max(1, confirmationsCount))
                                        .SelectMany(o => o.ReceivedCoins).ToList();
            
            //get unique saved coins
            if (confirmationsCount == 0)
            {
                var set = new HashSet<OutPoint>(coins.Select(x => x.Outpoint));

                var internalSavedOutputs = (await _broadcastedOutputRepository.GetOutputs(walletAddress))
                    .Where(o => !set.Contains(new OutPoint(uint256.Parse(o.TransactionHash), o.N)));

                coins.AddRange(internalSavedOutputs.Select(o =>
                {
                    var coin = new Coin(new OutPoint(uint256.Parse(o.TransactionHash), o.N),
                        new TxOut(new Money(o.Amount, MoneyUnit.Satoshi), o.ScriptPubKey.ToScript()));
                    if (o.AssetId != null)
                        return
                            (ICoin)
                            coin.ToColoredCoin(new BitcoinAssetId(o.AssetId, _connectionParams.Network).AssetId,
                                (ulong)o.Quantity);
                    return coin;
                }));
            }

            var unspentOutputs = await _spentOutputRepository.GetUnspentOutputs(coins.Select(o => new Output(o.Outpoint)));

            var unspentSet = new HashSet<OutPoint>(unspentOutputs.Select(x => new OutPoint(uint256.Parse(x.TransactionHash), x.N)));

            coins = coins.Where(o => unspentSet.Contains(o.Outpoint)).ToList();

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

        public async Task<IEnumerable<ICoin>> GetUncoloredUnspentOutputs(string walletAddress, int confirmationsCount = 0)
        {
            return (await GetUnspentOutputs(walletAddress, confirmationsCount)).OfType<Coin>();
        }

        public async Task<IEnumerable<ColoredCoin>> GetColoredUnspentOutputs(string walletAddress, AssetId assetIdObj, int confirmationsCount = 0)
        {
            return (await GetUnspentOutputs(walletAddress, confirmationsCount)).OfType<ColoredCoin>().Where(o => o.AssetId == assetIdObj);
        }

        public async Task<IEnumerable<ColoredCoin>> GetColoredUnspentOutputs(string walletAddress, int confirmationsCount = 0)
        {
            return (await GetUnspentOutputs(walletAddress, confirmationsCount)).OfType<ColoredCoin>().ToList();
        }
    }
}
