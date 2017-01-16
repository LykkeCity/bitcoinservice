using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Repositories.Assets;
using Core.Repositories.TransactionOutputs;
using LkeServices.Triggers.Attributes;

namespace BackgroundWorker.Functions
{
    public class PregeneratedOutputsRenewFunction
    {
        private readonly IPregeneratedOutputsQueueFactory _queueFactory;
        private readonly ILog _logger;
        private readonly IAssetRepository _assetRepository;

        public PregeneratedOutputsRenewFunction(IPregeneratedOutputsQueueFactory queueFactory, ILog logger,
            IAssetRepository assetRepository)
        {
            _queueFactory = queueFactory;
            _logger = logger;
            _assetRepository = assetRepository;
        }

        [TimerTrigger("24:00:00")]
        public async Task RenewPool()
        {
            await RenewFee();
            await RenewAssets();
        }

        private async Task RenewFee()
        {
            try
            {
                var queue = _queueFactory.CreateFeeQueue();
                var count = await queue.Count();
                for (int i = 0; i < count; i++)
                {
                    var coin = await queue.DequeueCoin();
                    if (coin == null)
                        return;
                    await queue.EnqueueOutputs(coin);
                }
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("PregeneratedOutputsRenewFunction", "RenewFee", "", e);
            }
        }

        private async Task RenewAssets()
        {
            var assets = (await _assetRepository.GetBitcoinAssets()).Where(o => !string.IsNullOrEmpty(o.AssetAddress) &&
                                                                                !o.IsDisabled &&
                                                                                string.IsNullOrWhiteSpace(o.PartnerId)).ToList();
            foreach (var asset in assets)
            {
                try
                {
                    var queue = _queueFactory.Create(asset.BlockChainAssetId);
                    var count = await queue.Count();
                    for (int i = 0; i < count; i++)
                    {
                        var coin = await queue.DequeueCoin();
                        if (coin == null)
                            return;
                        await queue.EnqueueOutputs(coin);
                    }
                }
                catch (Exception e)
                {
                    await _logger.WriteErrorAsync("PregeneratedOutputsRenewFunction", "RenewAssets", $"Asset {asset.Id}", e);
                }
            }
        }
    }
}
