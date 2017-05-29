﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Bitcoin;
using Core.Exceptions;
using Core.Notifiers;
using Core.OpenAssets;
using Core.Providers;
using Core.Repositories.Assets;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using LkeServices.Helpers;
using LkeServices.Providers;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace BitcoinJob.Functions
{
    public class GenerateOffchainOutputsFunction
    {
        private readonly BaseSettings _settings;
        private readonly CachedDataDictionary<string, IAsset> _assetRepostory;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly ILog _logger;
        private readonly ITransactionBuildHelper _transactionBuildHelper;
        private readonly RpcConnectionParams _connectionParams;
        private readonly TransactionBuildContextFactory _transactionBuildContextFactory;
        private readonly IBitcoinBroadcastService _bitcoinBroadcastService;
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly IEmailNotifier _emailNotifier;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ILykkeTransactionBuilderService _lykkeTransactionBuilderService;
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly ISignatureApiProvider _signatureApi;

        public GenerateOffchainOutputsFunction(BaseSettings settings, CachedDataDictionary<string, IAsset> assetRepostory,
            IBitcoinOutputsService bitcoinOutputsService,
            ILog logger,
            ITransactionBuildHelper transactionBuildHelper,
            RpcConnectionParams connectionParams,
            TransactionBuildContextFactory transactionBuildContextFactory,
            IBitcoinBroadcastService bitcoinBroadcastService,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            IEmailNotifier emailNotifier, ISlackNotifier slackNotifier,
            ILykkeTransactionBuilderService lykkeTransactionBuilderService,
            IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory)
        {
            _settings = settings;
            _assetRepostory = assetRepostory;
            _bitcoinOutputsService = bitcoinOutputsService;
            _logger = logger;
            _transactionBuildHelper = transactionBuildHelper;
            _connectionParams = connectionParams;
            _transactionBuildContextFactory = transactionBuildContextFactory;
            _bitcoinBroadcastService = bitcoinBroadcastService;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _emailNotifier = emailNotifier;
            _slackNotifier = slackNotifier;
            _lykkeTransactionBuilderService = lykkeTransactionBuilderService;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _signatureApi = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
        }

        [TimerTrigger("1:00:00")]
        public async Task Generate()
        {
            if (!_settings.Offchain.UseOffchainGeneration)
                return;
            await _logger.WriteInfoAsync("GenerateOffchainOutputsFunction", "Generate", null, "Start process");
            try
            {
                await GenerateIssueAllowedCoins();
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync("GenerateOffchainOutputsFunction", "Generate", "GenerateIssueAllowedCoins", ex);
            }
            try
            {
                await GenerateBtcOutputs();
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync("GenerateOffchainOutputsFunction", "Generate", "GenerateBtcOutputs", ex);
            }
            try
            {
                await GenerateColorOutputs("LKK");
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync("GenerateOffchainOutputsFunction", "Generate", "GenerateLKKOutputs", ex);
            }
            try
            {
                await GenerateColorOutputs("LKK1Y");
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync("GenerateOffchainOutputsFunction", "Generate", "GenerateLKK1YOutputs", ex);
            }
            await _logger.WriteInfoAsync("GenerateOffchainOutputsFunction", "Generate", null, "End process");
        }

        private Task GenerateColorOutputs(string assetId)
        {
            return Retry.Try(async () =>
            {
                var hotWallet = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(_settings.Offchain.HotWallet);
                var asset = await _assetRepostory.GetItemAsync(assetId);
                var assetIdObj = new BitcoinAssetId(asset.BlockChainAssetId).AssetId;

                var outputs = await _bitcoinOutputsService.GetColoredUnspentOutputs(_settings.Offchain.HotWallet, assetIdObj, 0, false);

                var balance = outputs.Aggregate(new AssetMoney(assetIdObj, 0), (accum, coin) => accum + coin.Amount);
                var outputSize = new AssetMoney(assetIdObj, _settings.Offchain.LkkOutputSize, asset.MultiplierPower);

                if (balance.ToDecimal(asset.MultiplierPower) < _settings.Offchain.MinLkkBalance)
                    await SendBalanceNotifications(assetId, _settings.Offchain.HotWallet, _settings.Offchain.MinLkkBalance);

                var existingCoinsCount = outputs.Count(o => o.Amount <= outputSize && o.Amount.Quantity > outputSize.Quantity / 2);

                if (existingCoinsCount > _settings.Offchain.MinCountOfLkkOutputs)
                    return;

                var generateCnt = _settings.Offchain.MaxCountOfLkkOutputs - existingCoinsCount;

                var coins = outputs.Where(o => o.Amount > outputSize * 2).ToList();

                balance = coins.Aggregate(new AssetMoney(assetIdObj, 0), (accum, coin) => accum + coin.Amount);

                generateCnt = Math.Min(generateCnt, (int)(balance.Quantity / outputSize.Quantity));
                if (generateCnt == 0)
                    return;

                await GenerateOutputs(generateCnt, coins, hotWallet, outputSize, asset);
            }, exception => (exception as BackendException)?.Code == ErrorCode.TransactionConcurrentInputsProblem, 3, _logger);
        }



        private Task GenerateBtcOutputs()
        {
            return Retry.Try(async () =>
            {
                var hotWallet = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(_settings.Offchain.HotWallet);

                var outputs = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(_settings.Offchain.HotWallet, 0, false)).Cast<Coin>().ToList();
                var balance = new Money(outputs.DefaultIfEmpty().Sum(o => o?.Amount ?? Money.Zero));
                var outputSize = new Money(_settings.Offchain.BtcOutpitSize, MoneyUnit.BTC);

                if (balance.ToDecimal(MoneyUnit.BTC) < _settings.Offchain.MinBtcBalance)
                    await SendBalanceNotifications("BTC", _settings.Offchain.HotWallet, _settings.Offchain.MinBtcBalance);


                var existingCoinsCount = outputs.Count(o => o.Amount <= outputSize && o.Amount > outputSize / 2);

                if (existingCoinsCount > _settings.Offchain.MinCountOfBtcOutputs)
                    return;

                var generateCnt = _settings.Offchain.MaxCountOfBtcOutputs - existingCoinsCount;

                var coins = outputs.Where(o => o.Amount > outputSize * 2).ToList();

                balance = coins.DefaultIfEmpty().Sum(o => o?.Amount ?? Money.Zero);

                generateCnt = Math.Min(generateCnt, (int)(balance / outputSize));
                if (generateCnt == 0)
                    return;
                await GenerateOutputs(generateCnt, coins, hotWallet, outputSize, await _assetRepostory.GetItemAsync("BTC"));
            }, exception => (exception as BackendException)?.Code == ErrorCode.TransactionConcurrentInputsProblem, 3, _logger);
        }

        private async Task GenerateOutputs(int generateCnt, IEnumerable<ICoin> coins, BitcoinAddress hotWallet, IMoney amount, IAsset asset)
        {
            var colored = amount is AssetMoney;
            await _logger.WriteInfoAsync("GenerateOffchainOutputsFunction", "GenerateOutputs", null,
                $"Start generate {generateCnt} outputs for {asset.Id}");

            var generated = 0;

            while (generated < generateCnt)
            {
                var outputsCount = Math.Min(_settings.Offchain.MaxSplittedOutputsInTransaction, generateCnt - generated);

                var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

                if (colored)
                {
                    var total = coins.Cast<ColoredCoin>().DefaultIfEmpty().Sum(o => o?.Amount.Quantity ?? 0);
                    if (total < ((AssetMoney)amount).Quantity * outputsCount)
                        return;
                }
                else
                {
                    var total = coins.Cast<Coin>().DefaultIfEmpty().Sum(o => o?.Amount ?? Money.Zero);
                    if (total < (Money)amount * outputsCount)
                        return;
                }

                await context.Build(async () =>
                {
                    var builder = new TransactionBuilder();

                    builder.AddCoins(coins);
                    for (int i = 0; i < outputsCount; i++)
                        if (colored)
                            builder.SendAsset(hotWallet, (AssetMoney)amount);
                        else
                            builder.Send(hotWallet, amount);

                    builder.SetChange(hotWallet, colored ? ChangeType.Colored : ChangeType.Uncolored);
                    await _transactionBuildHelper.AddFee(builder, context);
                    builder.SetChange(hotWallet, colored ? ChangeType.Colored : ChangeType.Uncolored);

                    var tr = builder.BuildTransaction(true);

                    await SignAndBroadcastTransaction(tr, context);

                    var usedCoins = new HashSet<OutPoint>(tr.Inputs.Select(o => o.PrevOut));

                    coins = coins.Where(o => !usedCoins.Contains(o.Outpoint)).ToList();

                    return "";
                });

                generated += outputsCount;
            }
            await _logger.WriteInfoAsync("GenerateOffchainOutputsFunction", "GenerateOutputs", null, "End process");

        }

        private async Task SendBalanceNotifications(string asset, string hotWallet, decimal minBalance)
        {
            string message = $"Offchain wallet {hotWallet} {asset} balance is less than {minBalance} {asset}!";
            await _logger.WriteWarningAsync("GenerateOffchainOutputsFunction", "SendBalanceNotifications", "", message);

            await _slackNotifier.FinanceWarningAsync(message);
            await _emailNotifier.WarningAsync("Bitcoin job", message);
        }

        private async Task GenerateIssueAllowedCoins()
        {
            var hotWallet = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(_settings.Offchain.HotWallet);
            var outputs = await _bitcoinOutputsService.GetColoredUnspentOutputs(_settings.Offchain.HotWallet);
            foreach (var asset in await _assetRepostory.Values())
            {
                if (OpenAssetsHelper.IsBitcoin(asset.Id) || OpenAssetsHelper.IsLkk(asset.Id) || !asset.IssueAllowed)
                    continue;
                try
                {
                    await _logger.WriteInfoAsync("GenerateOffchainOutputsFunction", "GenerateIssueAllowedCoins", "AssetId " + asset.Id, "Start process");

                    var assetId = new BitcoinAssetId(asset.BlockChainAssetId).AssetId;
                    var coins = outputs.Where(o => o.AssetId == assetId).ToList();

                    var minBalance = new AssetMoney(assetId, _settings.Offchain.MinIssueAllowedCoinBalance,
                        asset.MultiplierPower);

                    var balance = coins.Aggregate(new AssetMoney(assetId, 0), (accum, coin) => accum + coin.Amount);

                    if (balance > minBalance)
                        continue;

                    var maxBalance = new AssetMoney(assetId, _settings.Offchain.MaxIssueAllowedCoinBalance,
                        asset.MultiplierPower);
                    var cnt = (int)((maxBalance - balance).ToDecimal(asset.MultiplierPower) /
                                     _settings.Offchain.IssueAllowedCoinOutputSize) + 1;

                    var generated = 0;
                    while (generated < cnt)
                    {
                        var outputsCount = Math.Min(_settings.Offchain.MaxIssuedOutputsInTransaction, cnt - generated);

                        var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

                        await context.Build(async () =>
                        {

                            var builder = new TransactionBuilder();
                            var queue = _pregeneratedOutputsQueueFactory.Create(asset.BlockChainAssetId);
                            var coin = await queue.DequeueCoin();

                            try
                            {
                                var issueCoin = new IssuanceCoin(coin)
                                {
                                    DefinitionUrl = new Uri(asset.DefinitionUrl)
                                };

                                builder.AddCoins(issueCoin);
                                var outputSize = new AssetMoney(assetId, _settings.Offchain.IssueAllowedCoinOutputSize,
                                    asset.MultiplierPower);
                                for (var i = 0; i < outputsCount; i++)
                                    builder.IssueAsset(hotWallet, outputSize);
                                context.IssueAsset(assetId);
                                await _transactionBuildHelper.AddFee(builder, context);

                                var tr = builder.BuildTransaction(true);

                                await SignAndBroadcastTransaction(tr, context);

                                return "";
                            }
                            catch (Exception)
                            {
                                await queue.EnqueueOutputs(coin);

                                throw;
                            }
                        });

                        generated += outputsCount;
                    }
                }
                catch (Exception ex)
                {
                    await _logger.WriteErrorAsync("GenerateOffchainOutputsFunction", "GenerateIssueAllowedCoins", "AssetId " + asset.Id, ex);
                }
                finally
                {
                    await _logger.WriteInfoAsync("GenerateOffchainOutputsFunction", "GenerateIssueAllowedCoins", "AssetId " + asset.Id, "End process");
                }
            }
        }

        private async Task SignAndBroadcastTransaction(Transaction buildedTransaction, TransactionBuildContext context)
        {
            try
            {
                var id = Guid.NewGuid();

                var signed = await _signatureApi.SignTransaction(buildedTransaction.ToHex());

                var tr = new Transaction(signed);
                await _lykkeTransactionBuilderService.SaveSpentOutputs(id, tr);
                await SaveNewOutputs(id, buildedTransaction, context);

                await _bitcoinBroadcastService.BroadcastTransaction(id, tr);
            }
            catch (Exception)
            {
                await _lykkeTransactionBuilderService.RemoveSpenOutputs(buildedTransaction);
                throw;
            }
        }

        private async Task SaveNewOutputs(Guid transactionId, Transaction tr, TransactionBuildContext context)
        {
            var coloredOutputs = OpenAssetsHelper.OrderBasedColoringOutputs(tr, context);
            await _broadcastedOutputRepository.InsertOutputs(
                coloredOutputs.Select(o => new BroadcastedOutput(o, transactionId, _connectionParams.Network)).ToList());
        }
    }
}
