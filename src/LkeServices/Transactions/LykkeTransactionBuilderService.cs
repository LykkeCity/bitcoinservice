using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Bitcoin;
using Core.Exceptions;
using Core.OpenAssets;
using Core.Outputs;
using Core.Providers;
using Core.Repositories.Assets;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Core.Settings;
using Core.TransactionMonitoring;
using LkeServices.Helpers;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace LkeServices.Transactions
{
    public interface ILykkeTransactionBuilderService
    {
        Task<CreateTransactionResponse> GetTransferTransaction(BitcoinAddress source, BitcoinAddress destination, decimal amount, IAsset assetId, Guid transactionId, bool shouldReserveFee = false, bool sentDust = false);

        Task<CreateTransactionResponse> GetSwapTransaction(BitcoinAddress address1, decimal amount1, IAsset asset1,
            BitcoinAddress address2, decimal amount2, IAsset asset2, Guid transactionId);

        Task<CreateTransactionResponse> GetIssueTransaction(BitcoinAddress bitcoinAddres, decimal amount, IAsset asset, Guid transactionId);

        Task<CreateTransactionResponse> GetDestroyTransaction(BitcoinAddress bitcoinAddres, decimal modelAmount, IAsset asset, Guid transactionId);

        Task<CreateTransactionResponse> GetTransferAllTransaction(BitcoinAddress from, BitcoinAddress to, Guid transactionId);

        Task<CreateTransactionResponse> GetMultipleTransferTransaction(BitcoinAddress destination, IAsset asset, Dictionary<string, decimal> transferAddresses, int feeRate, decimal fixedFee, Guid transactionId);

        Task<Guid> AddTransactionId(Guid? transactionId, string rawRequest);

    }

    public class LykkeTransactionBuilderService : ILykkeTransactionBuilderService
    {
        private readonly ITransactionBuildHelper _transactionBuildHelper;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly ITransactionSignRequestRepository _signRequestRepository;
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly ILog _log;
        private readonly IFeeReserveMonitoringWriter _feeReserveMonitoringWriter;
        private readonly ISpentOutputService _spentOutputService;
        private readonly IOffchainService _offchainService;
        private readonly TransactionBuildContextFactory _transactionBuildContextFactory;
        private readonly CachedDataDictionary<string, IAsset> _assetRepository;

        private readonly RpcConnectionParams _connectionParams;
        private readonly BaseSettings _baseSettings;

        public LykkeTransactionBuilderService(
            ITransactionBuildHelper transactionBuildHelper,
            IBitcoinOutputsService bitcoinOutputsService,
            ITransactionSignRequestRepository signRequestRepository,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory,
            ILog log,
            IFeeReserveMonitoringWriter feeReserveMonitoringWriter,
            ISpentOutputService spentOutputService,
            IOffchainService offchainService,
            TransactionBuildContextFactory transactionBuildContextFactory,
            CachedDataDictionary<string, IAsset> assetRepository,
            RpcConnectionParams connectionParams, BaseSettings baseSettings)
        {
            _transactionBuildHelper = transactionBuildHelper;
            _bitcoinOutputsService = bitcoinOutputsService;
            _signRequestRepository = signRequestRepository;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _log = log;
            _feeReserveMonitoringWriter = feeReserveMonitoringWriter;
            _spentOutputService = spentOutputService;
            _offchainService = offchainService;
            _transactionBuildContextFactory = transactionBuildContextFactory;
            _assetRepository = assetRepository;

            _connectionParams = connectionParams;
            _baseSettings = baseSettings;
        }

        public Task<CreateTransactionResponse> GetTransferTransaction(BitcoinAddress sourceAddress,
            BitcoinAddress destAddress, decimal amount, IAsset asset, Guid transactionId, bool shouldReserveFee = false, bool sentDust = false)
        {
            return Retry.Try(async () =>
            {
                var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

                return await context.Build(async () =>
                {
                    var builder = new TransactionBuilder();

                    await TransferOneDirection(builder, context, sourceAddress, amount, asset, destAddress, !shouldReserveFee, sentDust);

                    await _transactionBuildHelper.AddFee(builder, context);

                    var buildedTransaction = builder.BuildTransaction(true);

                    await _spentOutputService.SaveSpentOutputs(transactionId, buildedTransaction);

                    await SaveNewOutputs(transactionId, buildedTransaction, context);

                    if (shouldReserveFee)
                        await _feeReserveMonitoringWriter.AddTransactionFeeReserve(transactionId, context.FeeCoins);

                    return new CreateTransactionResponse(buildedTransaction.ToHex(), transactionId);
                });
            }, exception => (exception as BackendException)?.Code == ErrorCode.TransactionConcurrentInputsProblem, 3, _log);
        }

        public Task<CreateTransactionResponse> GetSwapTransaction(BitcoinAddress address1, decimal amount1,
            IAsset asset1, BitcoinAddress address2, decimal amount2, IAsset asset2, Guid transactionId)
        {
            return Retry.Try(async () =>
            {
                var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

                return await context.Build(async () =>
                {
                    var builder = new TransactionBuilder();
                    await TransferOneDirection(builder, context, address1, amount1, asset1, address2);
                    await TransferOneDirection(builder, context, address2, amount2, asset2, address1);

                    await _transactionBuildHelper.AddFee(builder, context);

                    var buildedTransaction = builder.BuildTransaction(true);

                    await _spentOutputService.SaveSpentOutputs(transactionId, buildedTransaction);

                    await SaveNewOutputs(transactionId, buildedTransaction, context);

                    return new CreateTransactionResponse(buildedTransaction.ToHex(), transactionId);
                });
            }, exception => (exception as BackendException)?.Code == ErrorCode.TransactionConcurrentInputsProblem, 3, _log);
        }

        public Task<CreateTransactionResponse> GetIssueTransaction(BitcoinAddress bitcoinAddres, decimal amount, IAsset asset, Guid transactionId)
        {
            return Retry.Try(async () =>
            {
                var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

                return await context.Build(async () =>
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

                        var assetId = new BitcoinAssetId(asset.BlockChainAssetId, _connectionParams.Network).AssetId;

                        builder.AddCoins(issueCoin)
                            .IssueAsset(bitcoinAddres, new AssetMoney(assetId, amount, asset.MultiplierPower));
                        context.IssueAsset(assetId);

                        await _transactionBuildHelper.AddFee(builder, context);

                        var buildedTransaction = builder.BuildTransaction(true);

                        await _spentOutputService.SaveSpentOutputs(transactionId, buildedTransaction);

                        await SaveNewOutputs(transactionId, buildedTransaction, context);

                        return new CreateTransactionResponse(buildedTransaction.ToHex(), transactionId);
                    }
                    catch (Exception)
                    {
                        await queue.EnqueueOutputs(coin);
                        throw;
                    }
                });
            }, exception => (exception as BackendException)?.Code == ErrorCode.TransactionConcurrentInputsProblem, 3, _log);
        }

        public Task<CreateTransactionResponse> GetDestroyTransaction(BitcoinAddress bitcoinAddres, decimal modelAmount, IAsset asset, Guid transactionId)
        {
            return Retry.Try(async () =>
            {
                var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

                return await context.Build(async () =>
                {
                    var builder = new TransactionBuilder();

                    var assetId = new BitcoinAssetId(asset.BlockChainAssetId, _connectionParams.Network).AssetId;
                    var coins =
                        (await _bitcoinOutputsService.GetColoredUnspentOutputs(bitcoinAddres.ToString(), assetId)).ToList();

                    builder.SetChange(bitcoinAddres, ChangeType.Colored);
                    builder.AddCoins(coins);

                    var assetMoney = new AssetMoney(assetId, modelAmount, asset.MultiplierPower);

                    var changeAddress = BitcoinAddress.Create(_baseSettings.ChangeAddress, _connectionParams.Network);

                    _transactionBuildHelper.SendAssetWithChange(builder, context, coins, changeAddress, assetMoney, bitcoinAddres);

                    await _transactionBuildHelper.AddFee(builder, context);

                    var tx = builder.BuildTransaction(true);

                    OpenAssetsHelper.DestroyColorCoin(tx, assetMoney, changeAddress, _connectionParams.Network);

                    await _spentOutputService.SaveSpentOutputs(transactionId, tx);

                    await SaveNewOutputs(transactionId, tx, context);

                    return new CreateTransactionResponse(tx.ToHex(), transactionId);
                });
            }, exception => (exception as BackendException)?.Code == ErrorCode.TransactionConcurrentInputsProblem, 3, _log);
        }

        public Task<CreateTransactionResponse> GetTransferAllTransaction(BitcoinAddress @from, BitcoinAddress to, Guid transactionId)
        {
            return Retry.Try(async () =>
            {
                var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

                var channels = await _offchainService.GetCurrentChannels(from.ToString());

                var assets = await _assetRepository.Values();

                return await context.Build(async () =>
                {
                    var builder = new TransactionBuilder();
                    var uncoloredCoins = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(from.ToString())).ToList();
                    var coloredCoins = (await _bitcoinOutputsService.GetColoredUnspentOutputs(from.ToString())).ToList();

                    if (uncoloredCoins.Count == 0 && coloredCoins.Count == 0)
                        throw new BackendException("Address has no unspent outputs", ErrorCode.NoCoinsFound);

                    async Task<IDestination> GetChangeWallet(string asset)
                    {
                        var assetSetting = await _offchainService.GetAssetSetting(asset);
                        return OpenAssetsHelper.GetBitcoinAddressFormBase58Date(!string.IsNullOrEmpty(assetSetting.ChangeWallet) ? assetSetting.ChangeWallet
                            : assetSetting.HotWallet);
                    };

                    if (uncoloredCoins.Count > 0)
                    {
                        var hubAmount = Money.Zero;
                        IDestination hubAmountAddress = null;
                        var channel = channels.FirstOrDefault(o => o.Asset == "BTC");
                        if (channel != null)
                        {
                            hubAmount = Money.FromUnit(channel.HubAmount, MoneyUnit.BTC);
                            hubAmountAddress = await GetChangeWallet("BTC");
                        }

                        builder.AddCoins(uncoloredCoins);
                        context.AddCoins(uncoloredCoins);
                        builder.Send(to, uncoloredCoins.Sum(o => o.TxOut.Value) - hubAmount);
                        if (hubAmount > 0)
                            builder.Send(hubAmountAddress, hubAmount);
                    }
                    foreach (var assetGroup in coloredCoins.GroupBy(o => o.AssetId))
                    {
                        var asset = assets.First(o => o.BlockChainAssetId == assetGroup.Key.GetWif(_connectionParams.Network).ToString());

                        var channel = channels.FirstOrDefault(o => o.Asset == asset.Id);

                        var sum = new AssetMoney(assetGroup.Key);
                        foreach (var coloredCoin in assetGroup)
                            sum += coloredCoin.Amount;

                        var hubAmount = new AssetMoney(assetGroup.Key);
                        IDestination hubAmountAddress = null;
                        if (channel != null)
                        {
                            hubAmount = new AssetMoney(assetGroup.Key, channel.HubAmount, asset.MultiplierPower);
                            hubAmountAddress = await GetChangeWallet(asset.Id);
                        }
                        builder.AddCoins(assetGroup.ToList());
                        context.AddCoins(assetGroup.ToList());
                        builder.SendAsset(to, sum - hubAmount);
                        if (hubAmount.Quantity > 0)
                            builder.SendAsset(hubAmountAddress, hubAmount);
                    }
                    await _transactionBuildHelper.AddFee(builder, context);

                    var buildedTransaction = builder.BuildTransaction(true);

                    await _spentOutputService.SaveSpentOutputs(transactionId, buildedTransaction);

                    await SaveNewOutputs(transactionId, buildedTransaction, context);

                    foreach (var offchainChannel in channels)
                        await _offchainService.RemoveChannel(offchainChannel);

                    return new CreateTransactionResponse(buildedTransaction.ToHex(), transactionId);
                });
            }, exception => (exception as BackendException)?.Code == ErrorCode.TransactionConcurrentInputsProblem, 3, _log);
        }

        public Task<CreateTransactionResponse> GetMultipleTransferTransaction(BitcoinAddress destination, IAsset asset, Dictionary<string, decimal> transferAddresses, int feeRate, decimal fixedFee, Guid transactionId)
        {
            return Retry.Try(async () =>
            {
                var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

                return await context.Build(async () =>
                {
                    var builder = new TransactionBuilder();

                    builder.SetChange(destination, ChangeType.Uncolored);

                    foreach (var transferAddress in transferAddresses)
                    {
                        var source = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(transferAddress.Key);
                        await TransferOneDirection(builder, context, source, transferAddress.Value, asset, destination);
                    }

                    Transaction buildedTransaction;
                    try
                    {
                        buildedTransaction = builder.BuildTransaction(true);
                    }
                    catch (NotEnoughFundsException ex)
                    {
                        if (ex.Missing is Money)
                        {
                            var missingAmount = ((Money)ex.Missing).Satoshi;
                            _transactionBuildHelper.AddFakeInput(builder, new Money(missingAmount, MoneyUnit.Satoshi));
                            buildedTransaction = builder.BuildTransaction(true);
                        }
                        else throw;
                    }

                    _transactionBuildHelper.RemoveFakeInput(buildedTransaction);

                    _transactionBuildHelper.AggregateOutputs(buildedTransaction);

                    var fee = fixedFee > 0 ? Money.FromUnit(fixedFee, MoneyUnit.BTC) : await _transactionBuildHelper.CalcFee(buildedTransaction, feeRate);

                    foreach (var output in buildedTransaction.Outputs)
                    {
                        if (output.ScriptPubKey.GetDestinationAddress(_connectionParams.Network) == destination)
                        {
                            if (output.Value <= fee)
                                throw new BackendException("Amount is lower than fee", ErrorCode.NotEnoughBitcoinAvailable);
                            output.Value -= fee;
                            break;
                        }
                    }

                    await _spentOutputService.SaveSpentOutputs(transactionId, buildedTransaction);

                    await SaveNewOutputs(transactionId, buildedTransaction, context);

                    return new CreateTransactionResponse(buildedTransaction.ToHex(), transactionId);
                });
            }, exception => (exception as BackendException)?.Code == ErrorCode.TransactionConcurrentInputsProblem, 3, _log);
        }


        private async Task TransferOneDirection(TransactionBuilder builder, TransactionBuildContext context,
            BitcoinAddress @from, decimal amount, IAsset asset, BitcoinAddress to, bool addDust = true, bool sendDust = false)
        {
            var fromStr = from.ToString();

            if (OpenAssetsHelper.IsBitcoin(asset.Id))
            {
                var coins = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(fromStr)).ToList();
                var balance = coins.Cast<Coin>().Select(o => o.Amount).DefaultIfEmpty().Sum(o => o?.ToDecimal(MoneyUnit.BTC) ?? 0);
                if (sendDust && balance > amount &&
                    balance - amount < new TxOut(Money.Zero, from).GetDustThreshold(builder.StandardTransactionPolicy.MinRelayTxFee).ToDecimal(MoneyUnit.BTC))
                    amount = balance;
                await _transactionBuildHelper.SendWithChange(builder, context, coins, to, new Money(amount, MoneyUnit.BTC),
                    from, addDust);
            }
            else
            {
                var assetIdObj = new BitcoinAssetId(asset.BlockChainAssetId, _connectionParams.Network).AssetId;
                var assetAmount = new AssetMoney(assetIdObj, amount, asset.MultiplierPower);

                var coins = (await _bitcoinOutputsService.GetColoredUnspentOutputs(fromStr, assetIdObj)).ToList();
                _transactionBuildHelper.SendAssetWithChange(builder, context, coins, to, assetAmount, @from);
            }
        }

        private async Task SaveNewOutputs(Guid transactionId, Transaction tr, TransactionBuildContext context)
        {
            var coloredOutputs = OpenAssetsHelper.OrderBasedColoringOutputs(tr, context);
            await _broadcastedOutputRepository.InsertOutputs(
                    coloredOutputs.Select(o => new BroadcastedOutput(o, transactionId, _connectionParams.Network)).ToList());
        }



        public async Task<Guid> AddTransactionId(Guid? transactionId, string rawRequest)
        {
            await CheckDuplicatedTransactionId(transactionId);
            return await _signRequestRepository.InsertTransactionId(transactionId, rawRequest);
        }

        private async Task CheckDuplicatedTransactionId(Guid? transactionId)
        {
            if (!transactionId.HasValue)
                return;

            var data = await _signRequestRepository.GetSignRequest(transactionId.Value);

            if (data != null)
                throw new BackendException("Transaction with same id already exists", ErrorCode.DuplicateTransactionId);
        }
    }
}
