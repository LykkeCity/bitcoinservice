using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Helpers;
using Core.Performance;
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
        private readonly IInternalSpentOutputRepository _internalSpentOutputRepository;
        private readonly RpcConnectionParams _connectionParams;

        public BitcoinOutputsService(IQBitNinjaApiCaller qBitNinjaApiCaller,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            ISpentOutputRepository spentOutputRepository, IWalletAddressRepository walletAddressRepository,
            IInternalSpentOutputRepository internalSpentOutputRepository,
            RpcConnectionParams connectionParams)
        {
            _qBitNinjaApiCaller = qBitNinjaApiCaller;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _spentOutputRepository = spentOutputRepository;
            _walletAddressRepository = walletAddressRepository;
            _internalSpentOutputRepository = internalSpentOutputRepository;
            _connectionParams = connectionParams;
        }

        public async Task<IEnumerable<ICoin>> GetOnlyNinjaOutputs(string walletAddress, int minConfirmationsCount = 0, int maxConfirmationsCount = int.MaxValue)
        {
            var outputResponse = await _qBitNinjaApiCaller.GetAddressBalance(walletAddress);
            var coins = outputResponse.Operations
                .Where(x => x.Confirmations >= minConfirmationsCount && x.Confirmations <= maxConfirmationsCount)
                .SelectMany(o => o.ReceivedCoins).ToList();

            coins = await FilterCoins(coins, true, null);

            return await ToScriptCoins(walletAddress, coins);
        }


        public async Task<IEnumerable<ICoin>> GetUnspentOutputs(string walletAddress, int confirmationsCount = 0, bool useInternalSpentOutputs = true, IPerformanceMonitor monitor = null)
        {
            monitor?.Step("Get address balance");
            var outputResponse = await _qBitNinjaApiCaller.GetAddressBalance(walletAddress);
            var coins = outputResponse.Operations
                                        .Where(x => x.Confirmations >= Math.Max(1, confirmationsCount))
                                        .SelectMany(o => o.ReceivedCoins).ToList();

            await AddBroadcastedOutputs(coins, walletAddress, confirmationsCount, monitor);

            coins = await FilterCoins(coins, useInternalSpentOutputs, monitor);

            return await ToScriptCoins(walletAddress, coins);
        }

        private async Task<IEnumerable<ICoin>> ToScriptCoins(string walletAddress, List<ICoin> coins)
        {
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

        private async Task AddBroadcastedOutputs(List<ICoin> coins, string walletAddress, int confirmationsCount, IPerformanceMonitor monitor)
        {
            //get unique saved coins
            if (confirmationsCount == 0)
            {
                var set = new HashSet<OutPoint>(coins.Select(x => x.Outpoint));

                monitor?.Step("Get broadcasted outputs");
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
        }

        private async Task<List<ICoin>> FilterCoins(List<ICoin> coins, bool useInternalSpentOutputs, IPerformanceMonitor monitor)
        {
            monitor?.Step("Get unspent outputs");
            var unspentOutputs = await _spentOutputRepository.GetUnspentOutputs(coins.Select(o => new Output(o.Outpoint)));

            var unspentSet = new HashSet<OutPoint>(unspentOutputs.Select(x => new OutPoint(uint256.Parse(x.TransactionHash), x.N)));

            if (useInternalSpentOutputs)
            {
                monitor?.Step("Get internal spent outputs");
                var internalSpentOuputs =
                    new HashSet<OutPoint>(
                        (await _internalSpentOutputRepository.GetInternalSpentOutputs()).Select(x => new OutPoint(uint256.Parse(x.TransactionHash), x.N)));
                unspentSet.ExceptWith(internalSpentOuputs);
            }

            monitor?.Step("Filter ouputs");
            return coins.Where(o => unspentSet.Contains(o.Outpoint)).ToList();
        }

        public async Task<IEnumerable<ICoin>> GetUncoloredUnspentOutputs(string walletAddress, int confirmationsCount = 0, bool useInternalSpentOutputs = true, IPerformanceMonitor monitor = null)
        {
            return (await GetUnspentOutputs(walletAddress, confirmationsCount, useInternalSpentOutputs, monitor)).OfType<Coin>();
        }

        public async Task<IEnumerable<ColoredCoin>> GetColoredUnspentOutputs(string walletAddress, AssetId assetIdObj, int confirmationsCount = 0, bool useInternalSpentOutputs = true, IPerformanceMonitor monitor = null)
        {
            return (await GetUnspentOutputs(walletAddress, confirmationsCount, useInternalSpentOutputs, monitor)).OfType<ColoredCoin>().Where(o => o.AssetId == assetIdObj);
        }

        public async Task<IEnumerable<ColoredCoin>> GetColoredUnspentOutputs(string walletAddress, int confirmationsCount = 0, bool useInternalSpentOutputs = true, IPerformanceMonitor monitor = null)
        {
            return (await GetUnspentOutputs(walletAddress, confirmationsCount, useInternalSpentOutputs, monitor)).OfType<ColoredCoin>().ToList();
        }
    }
}
