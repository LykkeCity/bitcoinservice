using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Core.Bitcoin;
using Core.Exceptions;
using Core.Repositories.Assets;
using Core.Repositories.TransactionOutputs;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using QBitNinja.Client;

namespace EnqueueFees
{
    public class EnqueueFeesJob
    {
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly IAssetRepository _assetRepository;
        private string _feeAddress;

        public EnqueueFeesJob(IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory,
            IBitcoinOutputsService bitcoinOutputsService,
            IAssetRepository assetRepository, IConfiguration configuration)
        {
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _bitcoinOutputsService = bitcoinOutputsService;
            _assetRepository = assetRepository;
            _feeAddress = configuration.GetValue<string>("BitcoinConfig:FeeAddress");
        }


        public async Task Start(string type, string assetId)
        {
            if (type == "asset" && assetId == "all")
            {
                var assets = (await _assetRepository.GetBitcoinAssets()).Where(o => !string.IsNullOrEmpty(o.AssetAddress) &&
                                                                                    !o.IsDisabled &&
                                                                                    string.IsNullOrWhiteSpace(o.PartnerId)).ToList();
                foreach (var asset in assets)
                {
                    await RefreshOutputs(type, asset.Id);
                }
            }

            await RefreshOutputs(type, assetId);
        }

        private async Task RefreshOutputs(string type, string assetId)
        {
            Console.WriteLine($"Start process: type={type}" + (assetId != null ? $", asset={assetId}" : null));

            IPregeneratedOutputsQueue queue;
            string address;
            if (type == "fee")
            {
                queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();
                address = _feeAddress;
            }
            else
            {
                var asset = await _assetRepository.GetAssetById(assetId);
                queue = _pregeneratedOutputsQueueFactory.Create(asset.BlockChainAssetId);
                address = asset.AssetAddress;
                Console.WriteLine("BlockchainAssetId : " + asset.BlockChainAssetId);
            }

            var count = await queue.Count();

            Console.WriteLine($"Start collect {count} outputs from queue");

            var set = new HashSet<OutPoint>();

            while (count-- > 0)
            {
                Coin coin = null;
                try
                {
                    coin = await queue.DequeueCoin();

                    set.Add(coin.Outpoint);
                }
                finally
                {
                    await queue.EnqueueOutputs(coin);
                }
            }

            Console.WriteLine($"Coins collected");

            var coins = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(address)).OfType<Coin>().ToArray();

            Console.WriteLine($"Received {coins.Length} outputs from qbitninja");

            coins = coins.Where(x => !set.Contains(x.Outpoint)).ToArray();

            Console.WriteLine($"Got {coins.Length} missing outputs");

            await queue.EnqueueOutputs(coins);

            //Console.WriteLine("Start remove outputs from queue");
            //int i = 0;
            //while (true)
            //    try
            //    {
            //        await queue.DequeueCoin();
            //        i++;
            //    }
            //    catch (BackendException)
            //    {
            //        break;
            //    }
            //Console.WriteLine($"Removed {i} coins from queue");
            //var coins = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(address)).OfType<Coin>().ToArray();

            //Console.WriteLine($"Received {coins.Length} outputs from qbitninja");
            //Console.WriteLine("Start add coins to queue");

            //await queue.EnqueueOutputs(coins);

            Console.WriteLine("All coins successfuly added to queue");
            Console.WriteLine(Environment.NewLine);
        }
    }
}
