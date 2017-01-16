using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackgroundWorker.Commands;
using BackgroundWorker.Notifiers;
using Common.Log;
using Core.Bitcoin;
using Core.Exceptions;
using Core.Providers;
using Core.Repositories.Assets;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using LkeServices.Transactions;
using LkeServices.Triggers.Attributes;
using NBitcoin;

namespace BackgroundWorker.Functions
{
    public class GenerateOutputsFunction
    {
        private readonly Money _dustSize = new Money(2730);

        private bool _balanceWarningSended;

        private readonly IAssetRepository _assetRepository;
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly IFeeProvider _feeProvider;
        private readonly ISignatureApiProvider _signatureApiProvider;
        private readonly IRpcBitcoinClient _bitcoinClient;
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly ILykkeTransactionBuilderService _lykkeTransactionBuilderService;
        private readonly ILog _logger;
        private readonly BaseSettings _baseSettings;
        private readonly RpcConnectionParams _connectionParams;
        private readonly IEmailNotifier _emailNotifier;
        private readonly ISlackNotifier _slackNotifier;

        public GenerateOutputsFunction(IAssetRepository assetRepository,
            IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory,
            IBitcoinOutputsService bitcoinOutputsService,
            IFeeProvider feeProvider,
            ISignatureApiProvider signatureApiProvider,
            IRpcBitcoinClient bitcoinClient,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            ILykkeTransactionBuilderService lykkeTransactionBuilderService,
            BaseSettings baseSettings, RpcConnectionParams connectionParams, ILog logger, IEmailNotifier emailNotifier, ISlackNotifier slackNotifier)
        {
            _assetRepository = assetRepository;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _bitcoinOutputsService = bitcoinOutputsService;
            _feeProvider = feeProvider;
            _signatureApiProvider = signatureApiProvider;
            _bitcoinClient = bitcoinClient;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _lykkeTransactionBuilderService = lykkeTransactionBuilderService;
            _baseSettings = baseSettings;
            _connectionParams = connectionParams;
            _logger = logger;
            _emailNotifier = emailNotifier;
            _slackNotifier = slackNotifier;
        }


        [TimerTrigger("00:01:00")]
        public async Task GenerateOutputs()
        {
            await InternalBalanceCheck();
            await GenerateFeeOutputs();
            await GenerateAssetOutputs();
        }

        private async Task GenerateFeeOutputs()
        {
            var queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();

            var uncoloredOutputs = await _bitcoinOutputsService.GetUncoloredUnspentOutputs(_baseSettings.HotWalletForPregeneratedOutputs);

            var outputs = uncoloredOutputs.ToList();

            while (await queue.Count() < _baseSettings.MinPregeneratedOutputsCount)
            {
                var totalRequiredAmount = Money.FromUnit(_baseSettings.GenerateOutputsBatchSize * _baseSettings.PregeneratedFeeAmount, MoneyUnit.BTC); // Convert to satoshi

                var feeAmount = new Money(_baseSettings.PregeneratedFeeAmount, MoneyUnit.BTC);

                if (outputs.Sum(o => o.TxOut.Value) < totalRequiredAmount)
                    throw new BackendException($"The sum of total applicable outputs is less than the required: {totalRequiredAmount} satoshis.", ErrorCode.NotEnoughBitcoinAvailable);

                var hotWallet = new BitcoinPubKeyAddress(_baseSettings.HotWalletForPregeneratedOutputs, _connectionParams.Network);
                var builder = new TransactionBuilder();

                builder.AddCoins(outputs);
                builder.SetChange(hotWallet);

                for (var i = 0; i < _baseSettings.GenerateOutputsBatchSize; i++)
                    builder.Send(new BitcoinPubKeyAddress(_baseSettings.FeeAddress, _connectionParams.Network), feeAmount);

                builder.SendFees(await _feeProvider.CalcFeeForTransaction(builder));

                var signedHex = await _signatureApiProvider.SignTransaction(builder.BuildTransaction(false).ToHex());

                var signedTr = new Transaction(signedHex);

                await _bitcoinClient.BroadcastTransaction(signedTr);

                await queue.EnqueueOutputs(signedTr.Outputs.AsCoins().Where(o => o.TxOut.Value == feeAmount).ToArray());

                await FinishOutputs(signedTr, hotWallet);
            }
        }

        private async Task GenerateAssetOutputs()
        {
            var hotWallet = new BitcoinPubKeyAddress(_baseSettings.HotWalletForPregeneratedOutputs, _connectionParams.Network);
            var assets = (await _assetRepository.GetBitcoinAssets()).Where(o => !string.IsNullOrEmpty(o.AssetAddress) &&
                                                                                !o.IsDisabled &&
                                                                                string.IsNullOrWhiteSpace(o.PartnerId)).ToList();
            foreach (var asset in assets)
            {
                try
                {
                    if (!_baseSettings.IssuedAssets.Contains(asset.Id))
                        continue;
                    if (asset.DefinitionUrl == null)
                    {
                        await _logger.WriteWarningAsync("GenerateOutputsFunction", "GenerateAssetOutputs", $"Asset: {asset.Id} has no DefinitionUrl", "");
                        continue;
                    }
                    var queue = _pregeneratedOutputsQueueFactory.Create(asset.BlockChainAssetId);
                    while (await queue.Count() < _baseSettings.MinPregeneratedAssetOutputsCount)
                    {
                        var coins = await _bitcoinOutputsService.GetUncoloredUnspentOutputs(hotWallet.ToWif());
                        TransactionBuilder builder = new TransactionBuilder();

                        builder.AddCoins(coins);
                        for (var i = 0; i < _baseSettings.GenerateAssetOutputsBatchSize; i++)
                            builder.Send(new BitcoinPubKeyAddress(asset.AssetAddress, _connectionParams.Network), _dustSize);
                        builder.SetChange(hotWallet);

                        builder.SendFees(await _feeProvider.CalcFeeForTransaction(builder));
                        var signedHex = await _signatureApiProvider.SignTransaction(builder.BuildTransaction(true).ToHex());

                        var signedTr = new Transaction(signedHex);
                        await _bitcoinClient.BroadcastTransaction(signedTr);

                        await queue.EnqueueOutputs(signedTr.Outputs.AsCoins()
                            .Where(o => o.ScriptPubKey.GetDestinationAddress(_connectionParams.Network).ToWif() == asset.AssetAddress &&
                                        o.TxOut.Value == _dustSize).ToArray());

                        await FinishOutputs(signedTr, hotWallet);
                    }
                }
                catch (Exception e)
                {
                    await _logger.WriteErrorAsync("GenerateOutputsFunction", "GenerateAssetOutputs", $"Asset {asset.Id}", e);
                }
            }
        }


        private async Task FinishOutputs(Transaction tr, BitcoinPubKeyAddress hotWallet)
        {
            var transactionId = Guid.NewGuid();

            await _broadcastedOutputRepository.InsertOutputs(tr.Outputs.AsCoins()
                .Where(o => o.ScriptPubKey.GetDestinationAddress(_connectionParams.Network).ToWif() == hotWallet.ToWif())
                .Select(o => new BroadcastedOutput(o, transactionId, _connectionParams.Network)));
            await _broadcastedOutputRepository.SetTransactionHash(transactionId, tr.GetHash().ToString());

            await _lykkeTransactionBuilderService.SaveSpentOutputs(tr);
        }

        private async Task InternalBalanceCheck()
        {
            try
            {
                var coins = await _bitcoinOutputsService.GetUncoloredUnspentOutputs(_baseSettings.HotWalletForPregeneratedOutputs);
                var balance = coins.OfType<Coin>().Sum(x => x.Amount);

                if (balance < _baseSettings.MinHotWalletBalance)
                {
                    string message =
                        $"Hot wallet {_baseSettings.HotWalletForPregeneratedOutputs} balance is less that {_baseSettings.MinHotWalletBalance} BTC !";
                    await _logger.WriteWarningAsync("GenerateOutputsFunction", "InternalBalanceCheck", "", message);

                    if (!_balanceWarningSended)
                    {
                        await _slackNotifier.WarningAsync(message);
                        await _emailNotifier.WarningAsync("Bitcoin job", message);
                    }

                    _balanceWarningSended = true;
                }
                else
                {
                    // reset if balance become higher
                    _balanceWarningSended = false;
                }
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("GenerateOutputsFunction", "InternalBalanceCheck", "", e);
            }
        }
    }
}
