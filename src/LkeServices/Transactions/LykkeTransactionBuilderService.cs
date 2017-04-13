using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Bitcoin;
using Core.Exceptions;
using Core.OpenAssets;
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
        Task<CreateTransactionResponse> GetTransferTransaction(BitcoinAddress source, BitcoinAddress destination, decimal amount, IAsset assetId, Guid transactionId, bool shouldReserveFee = false);

        Task<CreateTransactionResponse> GetSwapTransaction(BitcoinAddress address1, decimal amount1, IAsset asset1,
            BitcoinAddress address2, decimal amount2, IAsset asset2, Guid transactionId);

        Task<CreateTransactionResponse> GetIssueTransaction(BitcoinAddress bitcoinAddres, decimal amount, IAsset asset, Guid transactionId);

        Task<CreateTransactionResponse> GetDestroyTransaction(BitcoinAddress bitcoinAddres, decimal modelAmount, IAsset asset, Guid transactionId);

        Task<CreateTransactionResponse> GetTransferAllTransaction(BitcoinAddress from, BitcoinAddress to, Guid transactionId);

        Task SaveSpentOutputs(Guid transactionId, Transaction transaction);

        Task RemoveSpenOutputs(Transaction transaction);

        Task<Guid> AddTransactionId(Guid? transactionId, string rawRequest);
    }

    public class LykkeTransactionBuilderService : ILykkeTransactionBuilderService
    {
        private readonly ITransactionBuildHelper _transactionBuildHelper;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly ITransactionSignRequestRepository _signRequestRepository;
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly ISpentOutputRepository _spentOutputRepository;
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly ILog _log;
        private readonly IFeeReserveMonitoringWriter _feeReserveMonitoringWriter;
        private readonly TransactionBuildContextFactory _transactionBuildContextFactory;

        private readonly RpcConnectionParams _connectionParams;
        private readonly BaseSettings _baseSettings;

        public LykkeTransactionBuilderService(
            ITransactionBuildHelper transactionBuildHelper,
            IBitcoinOutputsService bitcoinOutputsService,
            ITransactionSignRequestRepository signRequestRepository,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            ISpentOutputRepository spentOutputRepository,
            IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory,
            ILog log,
            IFeeReserveMonitoringWriter feeReserveMonitoringWriter,
            TransactionBuildContextFactory transactionBuildContextFactory,
            RpcConnectionParams connectionParams, BaseSettings baseSettings)
        {
            _transactionBuildHelper = transactionBuildHelper;
            _bitcoinOutputsService = bitcoinOutputsService;
            _signRequestRepository = signRequestRepository;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _spentOutputRepository = spentOutputRepository;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _log = log;
            _feeReserveMonitoringWriter = feeReserveMonitoringWriter;
            _transactionBuildContextFactory = transactionBuildContextFactory;

            _connectionParams = connectionParams;
            _baseSettings = baseSettings;
        }

        public Task<CreateTransactionResponse> GetTransferTransaction(BitcoinAddress sourceAddress,
            BitcoinAddress destAddress, decimal amount, IAsset asset, Guid transactionId, bool shouldReserveFee = false)
        {
            return Retry.Try(async () =>
            {
                var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

                return await context.Build(async () =>
                {
                    var builder = new TransactionBuilder();

                    await TransferOneDirection(builder, context, sourceAddress, amount, asset, destAddress, !shouldReserveFee);

                    await _transactionBuildHelper.AddFee(builder, context);

                    var buildedTransaction = builder.BuildTransaction(true);

                    await SaveSpentOutputs(transactionId, buildedTransaction);

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

                    await SaveSpentOutputs(transactionId, buildedTransaction);

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

                        await SaveSpentOutputs(transactionId, buildedTransaction);

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
                        (await _bitcoinOutputsService.GetColoredUnspentOutputs(bitcoinAddres.ToWif(), assetId)).ToList();

                    builder.SetChange(bitcoinAddres, ChangeType.Colored);
                    builder.AddCoins(coins);

                    var assetMoney = new AssetMoney(assetId, modelAmount, asset.MultiplierPower);

                    var changeAddress = BitcoinAddress.Create(_baseSettings.ChangeAddress, _connectionParams.Network);

                    _transactionBuildHelper.SendAssetWithChange(builder, context, coins, changeAddress, assetMoney, bitcoinAddres);

                    await _transactionBuildHelper.AddFee(builder, context);

                    var tx = builder.BuildTransaction(true);

                    uint markerPosition;
                    var colorMarker = ColorMarker.Get(tx, out markerPosition);

                    for (var i = 0; i < colorMarker.Quantities.Length; i++)
                    {
                        if ((long)colorMarker.Quantities[i] == assetMoney.Quantity &&
                            tx.Outputs[i + 1].ScriptPubKey.GetDestinationAddress(_connectionParams.Network) == changeAddress)
                        {
                            colorMarker.Quantities[i] = 0;
                            break;
                        }
                    }

                    tx.Outputs[markerPosition].ScriptPubKey = colorMarker.GetScript();

                    await SaveSpentOutputs(transactionId, tx);

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

                return await context.Build(async () =>
                {
                    var builder = new TransactionBuilder();
                    var uncoloredCoins = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(from.ToWif())).ToList();
                    var coloredCoins = (await _bitcoinOutputsService.GetColoredUnspentOutputs(from.ToWif())).ToList();

                    if (uncoloredCoins.Count == 0 && coloredCoins.Count == 0)
                        throw new BackendException("Address has no unspent outputs", ErrorCode.NoCoinsFound);

                    if (uncoloredCoins.Count > 0)
                        await _transactionBuildHelper.SendWithChange(builder, context, uncoloredCoins, to, uncoloredCoins.Sum(o => o.TxOut.Value), from);
                    foreach (var assetGroup in coloredCoins.GroupBy(o => o.AssetId))
                    {
                        var sum = new AssetMoney(assetGroup.Key);
                        foreach (var coloredCoin in assetGroup)
                            sum += coloredCoin.Amount;

                        _transactionBuildHelper.SendAssetWithChange(builder, context, assetGroup.ToList(), to, sum, from);
                    }
                    await _transactionBuildHelper.AddFee(builder, context);

                    var buildedTransaction = builder.BuildTransaction(true);

                    await SaveSpentOutputs(transactionId, buildedTransaction);

                    await SaveNewOutputs(transactionId, buildedTransaction, context);

                    return new CreateTransactionResponse(buildedTransaction.ToHex(), transactionId);
                });
            }, exception => (exception as BackendException)?.Code == ErrorCode.TransactionConcurrentInputsProblem, 3, _log);
        }


        private async Task TransferOneDirection(TransactionBuilder builder, TransactionBuildContext context,
            BitcoinAddress @from, decimal amount, IAsset asset, BitcoinAddress to, bool addDust = true)
        {
            var fromStr = from.ToWif();

            if (OpenAssetsHelper.IsBitcoin(asset.Id))
            {
                var coins = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(fromStr)).ToList();
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

        public async Task SaveSpentOutputs(Guid transactionId, Transaction transaction)
        {
            await _spentOutputRepository.InsertSpentOutputs(transactionId, transaction.Inputs.Select(o => new Output(o.PrevOut)));
            foreach (var outPoint in transaction.Inputs.Select(o => o.PrevOut))
            {
                await _broadcastedOutputRepository.DeleteOutput(outPoint.Hash.ToString(), (int)outPoint.N);
            }
        }

        public Task RemoveSpenOutputs(Transaction transaction)
        {
            return _spentOutputRepository.RemoveSpentOutputs(transaction.Inputs.Select(o => new Output(o.PrevOut)));
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
