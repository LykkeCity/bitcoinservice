using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories.TransactionOutputs;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace Core.OpenAssets
{
    public class TransactionBuildContext
    {
        private readonly Network _network;
        public Network Network => _network;

        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;

        public TransactionBuildContext(Network network, IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory)
        {
            _network = network;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
        }


        public List<ICoin> Coins { get; set; } = new List<ICoin>();

        public List<ICoin> FeeCoins { get; set; } = new List<ICoin>();

        public AssetId IssuedAssetId { get; private set; }

        public void AddCoins(IEnumerable<ICoin> coins, bool feeCoin = false)
        {
            AddCoins(feeCoin, coins.ToArray());
        }

        public void AddCoins(bool feeCoin = false, params ICoin[] coins)
        {
            Coins.AddRange(coins);
            if (feeCoin)
                FeeCoins.AddRange(coins);
        }

        public void IssueAsset(AssetId asset)
        {
            IssuedAssetId = asset;
        }

        public IEnumerable<long> GetAssetAmounts
        {
            get
            {
                return Coins.Select(o =>
                {
                    var colorCoin = o as ColoredCoin;
                    return colorCoin?.Amount.Quantity ?? 0;
                });
            }
        }



        public IEnumerable<string> GetAssetIds()
        {
            return Coins.Select(o =>
            {
                var colorCoin = o as ColoredCoin;
                return colorCoin?.Amount.Id.GetWif(_network).ToString();
            });
        }


        public async Task<T> Build<T>(Func<Task<T>> buildAction)
        {
            try
            {
                return await buildAction();
            }
            catch (Exception)
            {
                if (FeeCoins.Count > 0)
                {
                    var queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();
                    await queue.EnqueueOutputs(FeeCoins.OfType<Coin>().ToArray());
                }
                throw;
            }
        }
    }
}
