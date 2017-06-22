using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Bitcoin;
using Core.Exceptions;
using Core.Notifiers;
using Core.Outputs;
using Core.Providers;
using Core.Repositories.Assets;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using LkeServices.Providers;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using NBitcoin;

namespace BitcoinJob.Functions
{
    public class GenerateOutputsFunction
    {
        private readonly Money _dustSize = new Money(2730);

        private static DateTime _lastWarningSentTime = DateTime.MinValue;
        private static DateTime _lastWarningFeeSentTime = DateTime.MinValue;

        private readonly IAssetRepository _assetRepository;
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly IFeeProvider _feeProvider;
        private readonly ISignatureApiProvider _signatureApiProvider;
        private readonly IRpcBitcoinClient _bitcoinClient;
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly ISpentOutputService _spentOutputService;        
        private readonly ILog _logger;
        private readonly BaseSettings _baseSettings;
        private readonly RpcConnectionParams _connectionParams;
        private readonly IEmailNotifier _emailNotifier;
        private readonly ISlackNotifier _slackNotifier;

        public GenerateOutputsFunction(IAssetRepository assetRepository,
            IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory,
            IBitcoinOutputsService bitcoinOutputsService,
            IFeeProvider feeProvider,
            IRpcBitcoinClient bitcoinClient,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            ISpentOutputService spentOutputService,
            BaseSettings baseSettings, RpcConnectionParams connectionParams, ILog logger, IEmailNotifier emailNotifier, ISlackNotifier slackNotifier, Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory)
        {
            _assetRepository = assetRepository;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _bitcoinOutputsService = bitcoinOutputsService;
            _feeProvider = feeProvider;
            _bitcoinClient = bitcoinClient;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _spentOutputService = spentOutputService;
            _baseSettings = baseSettings;
            _connectionParams = connectionParams;
            _logger = logger;
            _emailNotifier = emailNotifier;
            _slackNotifier = slackNotifier;
            _signatureApiProvider = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
        }


        [TimerTrigger("00:30:00")]
        public async Task GenerateOutputs()
        {
            await InternalBalanceCheck();
            await GenerateFeeOutputs();
            await GenerateAssetOutputs();
        }

        private async Task GenerateFeeOutputs()
        {
            await _logger.WriteInfoAsync("GenerateOutputsFunction", "GenerateFeeOutputs", null, "Start process");
            var queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();

            try
            {
                while (await queue.Count() < _baseSettings.MinPregeneratedOutputsCount)
                {
                    var uncoloredOutputs = await _bitcoinOutputsService.GetUncoloredUnspentOutputs(_baseSettings.HotWalletForPregeneratedOutputs);

                    var outputs = uncoloredOutputs.ToList();

                    var totalRequiredAmount = Money.FromUnit(_baseSettings.GenerateOutputsBatchSize * _baseSettings.PregeneratedFeeAmount,
                        MoneyUnit.BTC); // Convert to satoshi

                    var feeAmount = new Money(_baseSettings.PregeneratedFeeAmount, MoneyUnit.BTC);

                    if (outputs.Sum(o => o.TxOut.Value) < totalRequiredAmount)
                        throw new BackendException($"The sum of total applicable outputs is less than the required: {totalRequiredAmount} satoshis.",
                            ErrorCode.NotEnoughBitcoinAvailable);

                    var hotWallet = new BitcoinPubKeyAddress(_baseSettings.HotWalletForPregeneratedOutputs, _connectionParams.Network);
                    var builder = new TransactionBuilder();

                    builder.AddCoins(outputs);
                    builder.SetChange(hotWallet);

                    for (var i = 0; i < _baseSettings.GenerateOutputsBatchSize; i++)
                        builder.Send(new BitcoinPubKeyAddress(_baseSettings.FeeAddress, _connectionParams.Network), feeAmount);

                    builder.SendFees(await _feeProvider.CalcFeeForTransaction(builder));

                    var signedHex = await _signatureApiProvider.SignTransaction(builder.BuildTransaction(false).ToHex());

                    var signedTr = new Transaction(signedHex);

                    var transactionId = Guid.NewGuid();

                    await _bitcoinClient.BroadcastTransaction(signedTr, transactionId);

                    await queue.EnqueueOutputs(signedTr.Outputs.AsCoins().Where(o => o.TxOut.Value == feeAmount).ToArray());

                    await FinishOutputs(transactionId, signedTr, hotWallet);
                }
            }
            finally
            {
                if (await queue.Count() < _baseSettings.MinPregeneratedOutputsCount && (DateTime.UtcNow - _lastWarningFeeSentTime).TotalHours > 1)
                {
                    var message = $"Count of fee outputs is less than {_baseSettings.MinPregeneratedOutputsCount}";
                    await _slackNotifier.FinanceWarningAsync(message);
                    await _emailNotifier.WarningAsync("Bitcoin job", message);
                    _lastWarningFeeSentTime = DateTime.UtcNow;
                }
                await _logger.WriteInfoAsync("GenerateOutputsFunction", "GenerateFeeOutputs", null, "End process");
            }
        }

        private async Task GenerateAssetOutputs()
        {
            await _logger.WriteInfoAsync("GenerateOutputsFunction", "GenerateAssetOutputs", null, "Start process");
            var hotWallet = new BitcoinPubKeyAddress(_baseSettings.HotWalletForPregeneratedOutputs, _connectionParams.Network);
            var assets = (await _assetRepository.GetBitcoinAssets()).Where(o => !string.IsNullOrEmpty(o.AssetAddress) &&
                                                                                !o.IsDisabled &&
                                                                                o.IssueAllowed).ToList();
            foreach (var asset in assets)
            {
                try
                {
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
                        var transactionId = Guid.NewGuid();
                        await _bitcoinClient.BroadcastTransaction(signedTr, transactionId);

                        await queue.EnqueueOutputs(signedTr.Outputs.AsCoins()
                            .Where(o => o.ScriptPubKey.GetDestinationAddress(_connectionParams.Network).ToWif() == asset.AssetAddress &&
                                        o.TxOut.Value == _dustSize).ToArray());

                        await FinishOutputs(transactionId, signedTr, hotWallet);
                    }
                }
                catch (Exception e)
                {
                    await _logger.WriteErrorAsync("GenerateOutputsFunction", "GenerateAssetOutputs", $"Asset {asset.Id}", e);
                }
            }
            await _logger.WriteInfoAsync("GenerateOutputsFunction", "GenerateAssetOutputs", null, "End process");
        }


        private async Task FinishOutputs(Guid transactionId, Transaction tr, BitcoinPubKeyAddress hotWallet)
        {

            await _broadcastedOutputRepository.InsertOutputs(tr.Outputs.AsCoins()
                .Where(o => o.ScriptPubKey.GetDestinationAddress(_connectionParams.Network).ToWif() == hotWallet.ToWif())
                .Select(o => new BroadcastedOutput(o, transactionId, _connectionParams.Network)));
            await _broadcastedOutputRepository.SetTransactionHash(transactionId, tr.GetHash().ToString());

            await _spentOutputService.SaveSpentOutputs(transactionId, tr);
        }

        private async Task InternalBalanceCheck()
        {
            var queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();
            await _logger.WriteInfoAsync("GenerateOutputsFunction", "InternalBalanceCheck", null, "Start process");
            try
            {
                var coins = await _bitcoinOutputsService.GetUncoloredUnspentOutputs(_baseSettings.HotWalletForPregeneratedOutputs);
                var balance = coins.OfType<Coin>().Sum(x => x.Amount);

                if (balance < new Money(_baseSettings.MinHotWalletBalance, MoneyUnit.BTC))
                {
                    if ((DateTime.UtcNow - _lastWarningSentTime).TotalHours > 1)
                    {
                        string message = $"Fees hot wallet {_baseSettings.HotWalletForPregeneratedOutputs} balance is less than {_baseSettings.MinHotWalletBalance} BTC! Pregenerated pool has {await queue.Count()} outputs.";
                        await _logger.WriteWarningAsync("GenerateOutputsFunction", "InternalBalanceCheck", "", message);

                        await _slackNotifier.FinanceWarningAsync(message);
                        await _emailNotifier.WarningAsync("Bitcoin job", message);

                        _lastWarningSentTime = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("GenerateOutputsFunction", "InternalBalanceCheck", "", e);
            }
            await _logger.WriteInfoAsync("GenerateOutputsFunction", "InternalBalanceCheck", null, "End process");
        }
    }
}
