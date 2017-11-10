using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common;
using Core;
using Core.Repositories.Assets;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;

namespace BitcoinJob.Functions
{
    public class BroadcastLastCommitmentFunction
    {
        private readonly IOffchainService _offchainService;
        private readonly CachedDataDictionary<string, IAsset> _assetCache;

        public BroadcastLastCommitmentFunction(IOffchainService offchainService, CachedDataDictionary<string, IAsset> assetCache)
        {
            _offchainService = offchainService;
            _assetCache = assetCache;
        }

        [QueueTrigger(Constants.CommitmentBroadcastQueue)]
        public async Task ProcessMessage(BroadcastCommitmentMessage msg, QueueTriggeringContext context)
        {
            var asset = await _assetCache.GetItemAsync(msg.Asset);
            if (asset == null)
            {
                msg.Error = "Asset is not found";
                context.MoveMessageToPoison(msg.ToJson());
                return;
            }

            await _offchainService.BroadcastCommitment(msg.Multisig, asset, msg.UseFees);
        }

    }

    public class BroadcastCommitmentMessage
    {
        public string Multisig { get; set; }

        public string Asset { get; set; }

        public bool UseFees { get; set; }

        public string Error { get; set; }
    }    
}
