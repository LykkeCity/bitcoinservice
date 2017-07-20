using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core;
using Core.Bitcoin;
using Core.Helpers;
using Core.Notifiers;
using Core.QBitNinja;
using Core.Repositories.Assets;
using Core.Repositories.Offchain;
using Core.Repositories.Settings;
using Core.Settings;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace BitcoinJob.Functions
{
    public class OffchainCommitmentMonitoringFunction
    {
        private readonly IQBitNinjaApiCaller _qBitNinjaApiCaller;
        private readonly ILog _logger;
        private readonly ICommitmentRepository _commitmentRepository;
        private readonly IOffchainService _offchainService;
        private readonly ISlackNotifier _slackNotifier;
        private readonly IAssetRepository _assetRepository;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IRpcBitcoinClient _rpcBitcoinClient;
        private readonly ICommitmentBroadcastRepository _commitmentBroadcastRepository;
        private readonly RpcConnectionParams _connectionParams;        

        public OffchainCommitmentMonitoringFunction(IQBitNinjaApiCaller qBitNinjaApiCaller, ILog logger, ICommitmentRepository commitmentRepository,
            IOffchainService offchainService,
            ISlackNotifier slackNotifier,
            IAssetRepository assetRepository,
            ISettingsRepository settingsRepository,
            IRpcBitcoinClient rpcBitcoinClient,
            ICommitmentBroadcastRepository commitmentBroadcastRepository,
            RpcConnectionParams connectionParams)
        {
            _qBitNinjaApiCaller = qBitNinjaApiCaller;
            _logger = logger;
            _commitmentRepository = commitmentRepository;
            _offchainService = offchainService;
            _slackNotifier = slackNotifier;
            _assetRepository = assetRepository;
            _settingsRepository = settingsRepository;
            _rpcBitcoinClient = rpcBitcoinClient;
            _commitmentBroadcastRepository = commitmentBroadcastRepository;
            _connectionParams = connectionParams;            
        }

        [TimerTrigger("00:01:00")]
        public async Task Monitoring()
        {
            var currentBlock = await _settingsRepository.Get<int>(Constants.ProcessingBlockSetting);
            if (currentBlock == 0)
                currentBlock = await _rpcBitcoinClient.GetBlockCount() - 1;
            var dbCommitments = (await _commitmentRepository.GetMonitoringCommitments()).GroupBy(o => o.LockedAddress).ToDictionary(o => o.Key, o => o);
            do
            {
                var block = await _qBitNinjaApiCaller.GetBlock(currentBlock);
                if (block == null)
                    break;
                foreach (var transaction in block.Block.Transactions)
                {
                    var marker = transaction.GetColoredMarker();

                    foreach (var transactionOutput in transaction.Outputs.AsIndexedOutputs())
                    {
                        var address = transactionOutput.TxOut.ScriptPubKey.GetDestinationAddress(_connectionParams.Network)?.ToWif();
                        if (address != null && dbCommitments.ContainsKey(address))
                        {
                            var commitments = dbCommitments[address].OrderByDescending(o => o.CreateDt).ToList();
                            ICoin coin = new Coin(transaction, transactionOutput.TxOut).ToScriptCoin(commitments[0].LockedScript.ToScript());

                            decimal amount;
                            if (marker == null)
                                amount = transactionOutput.TxOut.Value.ToDecimal(MoneyUnit.BTC);
                            else
                            {
                                var asset = await _assetRepository.GetAssetById(commitments[0].AssetId);
                                var assetMoney = new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId).AssetId, marker.Quantities[transactionOutput.N - 1]);
                                coin = ((Coin)coin).ToColoredCoin(assetMoney);
                                amount = assetMoney.ToDecimal(asset.MultiplierPower);
                            }
                            var commitment = commitments.FirstOrDefault(o => o.Type == CommitmentType.Hub && o.HubAmount == amount ||
                                                                             o.Type == CommitmentType.Client && o.ClientAmount == amount);

                            if (commitment != null)
                                await ProcessBroadcastedCommitment(commitment, coin);
                        }
                    }
                }
                currentBlock++;
                await _settingsRepository.Set(Constants.ProcessingBlockSetting, currentBlock);
            } while (true);
        }

        private async Task ProcessBroadcastedCommitment(ICommitment commitment, ICoin spendingCoin)
        {
            var lastCommitment = await _commitmentRepository.GetLastCommitment(commitment.Multisig, commitment.AssetId, commitment.Type);
            if (lastCommitment.CommitmentId == commitment.CommitmentId)
            {
                await _logger.WriteInfoAsync("OffchainCommitmentMonitoringFunction", "ProcessBroadcastedCommitment",
                        $"CommitmentId: {commitment.CommitmentId}", "Last commitment was broadcasted");

                await _offchainService.CloseChannel(commitment);
                await _commitmentBroadcastRepository.InsertCommitmentBroadcast(commitment.CommitmentId, spendingCoin.Outpoint.Hash.ToString(),
                    CommitmentBroadcastType.Valid, commitment.ClientAmount, commitment.HubAmount, commitment.ClientAmount, commitment.HubAmount, null);
                return;
            }
            await _logger.WriteWarningAsync("OffchainCommitmentMonitoringFunction", "ProcessBroadcastedCommitment",
                        $"CommitmentId: {commitment.CommitmentId}", "Commitment is not last.");
            if (commitment.Type == CommitmentType.Client)
            {
                var assetSettings = await _offchainService.GetAssetSetting(commitment.AssetId);
                var hash = await _offchainService.SpendCommitmemtByMultisig(commitment, spendingCoin, !string.IsNullOrEmpty(assetSettings.ChangeWallet) ? assetSettings.ChangeWallet : assetSettings.HotWallet);
                await _offchainService.CloseChannel(commitment);
                await _commitmentBroadcastRepository.InsertCommitmentBroadcast(commitment.CommitmentId, spendingCoin.Outpoint.Hash.ToString(),
                    CommitmentBroadcastType.Revoked, commitment.ClientAmount, commitment.HubAmount, lastCommitment.ClientAmount, lastCommitment.HubAmount, hash);
            }
            else
            {
                await _slackNotifier.ErrorAsync($"Hub commitment with id {commitment.CommitmentId} was broadcasted but it's not last");
            }
        }

    }
}
