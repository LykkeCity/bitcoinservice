using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories.ExtraAmounts;
using Core.Repositories.TransactionOutputs;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace Core.OpenAssets
{
    public class TransactionBuildContext
    {
        private readonly Network _network;        
        private readonly List<IExtraAmount> _extraAmounts = new List<IExtraAmount>();
        public Network Network => _network;

        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly IExtraAmountRepository _extraAmountRepository;

        public TransactionBuildContext(Network network, IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory
            , IExtraAmountRepository extraAmountRepository)
        {
            _network = network;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _extraAmountRepository = extraAmountRepository;
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

        public void AddExtraAmount(IExtraAmount extraAmount)
        {
            _extraAmounts.Add(extraAmount);
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
                foreach (var extraAmount in _extraAmounts)
                {
                    await _extraAmountRepository.Decrease(extraAmount);
                }
                throw;
            }
        }
    }
}
