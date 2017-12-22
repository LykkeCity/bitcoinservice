using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;
using Core.Bitcoin;
using Core.Helpers;
using Core.OpenAssets;
using Core.QBitNinja;
using Core.Repositories.Offchain;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using NBitcoin;

namespace BitcoinJob.Functions
{
    public class SpendBroadcastedCommitmentFunction
    {

        private readonly TimeSpan MessageProcessDelay = TimeSpan.FromMinutes(10);

        private readonly IOffchainService _offchainService;
        private readonly IQBitNinjaApiCaller _qBitNinjaApiCaller;
        private readonly ICommitmentRepository _commitmentRepository;
        private readonly RpcConnectionParams _connectionParams;
        private readonly ILog _log;

        public SpendBroadcastedCommitmentFunction(IOffchainService offchainService, IQBitNinjaApiCaller qBitNinjaApiCaller,
            ICommitmentRepository commitmentRepository, RpcConnectionParams connectionParams, ILog log)
        {
            _offchainService = offchainService;
            _qBitNinjaApiCaller = qBitNinjaApiCaller;
            _commitmentRepository = commitmentRepository;
            _connectionParams = connectionParams;
            _log = log;
        }

        [QueueTrigger(Constants.SpendCommitmentOutputQueue)]
        public async Task Process(SpendCommitmentMonitorindMessage message, QueueTriggeringContext context)
        {
            if (DateTime.UtcNow - message.LastTryTime.GetValueOrDefault() < MessageProcessDelay)
            {
                MoveToEnd(context, message);
                return;
            }
            message.LastTryTime = DateTime.UtcNow;

            var tr = await _qBitNinjaApiCaller.GetTransaction(message.TransactionHash);
            if (tr?.Block == null || tr.Block.Confirmations < OffchainService.OneDayDelay)
            {
                MoveToEnd(context, message);
                return;
            }
            var commitment = await _commitmentRepository.GetCommitment(message.CommitmentId);
            var lockedAddr = OpenAssetsHelper.ParseAddress(commitment.LockedAddress);
            var coin = tr.ReceivedCoins
                .FirstOrDefault(o => o.TxOut.ScriptPubKey.GetDestinationAddress(_connectionParams.Network) == lockedAddr);
            if (coin == null)
                throw new Exception("Not found coin for spending for " + message.ToJson());

            if (coin is Coin)
                coin = ((Coin)coin).ToScriptCoin(commitment.LockedScript.ToScript());
            else
            {
                var colored = coin as ColoredCoin;
                coin = colored.Bearer.ToScriptCoin(commitment.LockedScript.ToScript()).ToColoredCoin(colored.Amount);
            }

            var assetSettings = await _offchainService.GetAssetSetting(commitment.AssetId);
            try
            {
                var hash = await _offchainService.SpendCommitmemtByPubkey(commitment, coin,
                    !string.IsNullOrEmpty(assetSettings.ChangeWallet) ? assetSettings.ChangeWallet : assetSettings.HotWallet);
                await _log.WriteInfoAsync(nameof(SpendBroadcastedCommitmentFunction), nameof(Process), message.ToJson(),
                    "Spent commitment by transaction" + hash);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(SpendBroadcastedCommitmentFunction), nameof(Process), message.ToJson(), ex);
                MoveToEnd(context, message);
            }
        }

        private static void MoveToEnd(QueueTriggeringContext context, SpendCommitmentMonitorindMessage message)
        {
            context.MoveMessageToEnd(message.ToJson());
            context.SetCountQueueBasedDelay(10000, 100);
        }
    }
}
