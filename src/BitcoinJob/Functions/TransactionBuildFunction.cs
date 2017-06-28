using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Common.Log;
using Core;
using Core.Exceptions;
using Core.OpenAssets;
using Core.Repositories.Assets;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Core.Settings;
using Core.TransactionQueueWriter;
using Core.TransactionQueueWriter.Commands;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;

namespace BitcoinJob.Functions
{
    public class TransactionBuildFunction
    {
        private readonly ILykkeTransactionBuilderService _lykkeTransactionBuilderService;
        private readonly IAssetRepository _assetRepository;
        private readonly IFailedTransactionsManager _failedTransactionManager;
        private readonly Func<string, IQueueExt> _queueFactory;
        private readonly BaseSettings _settings;
        private readonly ILog _logger;
        private readonly ITransactionBlobStorage _transactionBlobStorage;
        private readonly ITransactionSignRequestRepository _signRequestRepository;

        public TransactionBuildFunction(ILykkeTransactionBuilderService lykkeTransactionBuilderService,
            IAssetRepository assetRepository,
            IFailedTransactionsManager failedTransactionManager,
            Func<string, IQueueExt> queueFactory, BaseSettings settings, ILog logger, ITransactionBlobStorage transactionBlobStorage,
            ITransactionSignRequestRepository signRequestRepository)
        {
            _lykkeTransactionBuilderService = lykkeTransactionBuilderService;
            _assetRepository = assetRepository;
            _failedTransactionManager = failedTransactionManager;
            _queueFactory = queueFactory;
            _settings = settings;
            _logger = logger;
            _transactionBlobStorage = transactionBlobStorage;
            _signRequestRepository = signRequestRepository;
        }


        [QueueTrigger(Constants.TransactionCommandQueue, 100, true)]
        public async Task ProcessMessage(TransactionQueueMessage message, QueueTriggeringContext context)
        {
            CreateTransactionResponse transactionResponse;
            try
            {
                var request = await _signRequestRepository.GetSignRequest(message.TransactionId);

                if (request?.Invalidated == true)
                {
                    context.MoveMessageToPoison(message.ToJson());
                    await _failedTransactionManager.InsertFailedTransaction(message.TransactionId, null, "Transaction was invalidated");
                    return;
                }

                switch (message.Type)
                {
                    case TransactionCommandType.Issue:
                        var issue = message.Command.DeserializeJson<IssueCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetIssueTransaction(OpenAssetsHelper.GetBitcoinAddressFormBase58Date(issue.Address),
                                                        issue.Amount, await _assetRepository.GetAssetById(issue.Asset), message.TransactionId);
                        break;
                    case TransactionCommandType.Transfer:
                        var transfer = message.Command.DeserializeJson<TransferCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetTransferTransaction(
                                                        OpenAssetsHelper.GetBitcoinAddressFormBase58Date(transfer.SourceAddress),
                                                        OpenAssetsHelper.GetBitcoinAddressFormBase58Date(transfer.DestinationAddress), transfer.Amount,
                                                        await _assetRepository.GetAssetById(transfer.Asset), message.TransactionId);
                        break;
                    case TransactionCommandType.TransferAll:
                        var transferAll = message.Command.DeserializeJson<TransferAllCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetTransferAllTransaction(
                                                        OpenAssetsHelper.GetBitcoinAddressFormBase58Date(transferAll.SourceAddress),
                                                        OpenAssetsHelper.GetBitcoinAddressFormBase58Date(transferAll.DestinationAddress),
                                                        message.TransactionId);
                        break;
                    case TransactionCommandType.Swap:
                        var swap = message.Command.DeserializeJson<SwapCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetSwapTransaction(
                                                        OpenAssetsHelper.GetBitcoinAddressFormBase58Date(swap.MultisigCustomer1),
                                                        swap.Amount1,
                                                        await _assetRepository.GetAssetById(swap.Asset1),
                                                        OpenAssetsHelper.GetBitcoinAddressFormBase58Date(swap.MultisigCustomer2),
                                                        swap.Amount2,
                                                        await _assetRepository.GetAssetById(swap.Asset2),
                                                        message.TransactionId);
                        break;
                    case TransactionCommandType.Destroy:
                        var destroy = message.Command.DeserializeJson<DestroyCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetDestroyTransaction(
                                                        OpenAssetsHelper.GetBitcoinAddressFormBase58Date(destroy.Address),
                                                        destroy.Amount,
                                                        await _assetRepository.GetAssetById(destroy.Asset),
                                                        message.TransactionId);
                        break;
                    case TransactionCommandType.MultipleTransfers:
                        var multipleTransfer = message.Command.DeserializeJson<MultipleTransferCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetMultipleTransferTransaction(
                            OpenAssetsHelper.GetBitcoinAddressFormBase58Date(multipleTransfer.Destination),
                            await _assetRepository.GetAssetById(multipleTransfer.Asset),
                            multipleTransfer.Addresses,
                            message.TransactionId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (BackendException e)
            {
                if (e.Code == ErrorCode.NoCoinsFound)
                    return;

                if (e.Text != message.LastError)
                    await _logger.WriteWarningAsync("TransactionBuildFunction", "ProcessMessage", $"Id: [{message.TransactionId}], cmd: [{message.Command}]", e.Text);

                message.LastError = e.Text;
                if (message.DequeueCount >= _settings.MaxDequeueCount)
                {
                    context.MoveMessageToPoison(message.ToJson());
                    await _failedTransactionManager.InsertFailedTransaction(message.TransactionId, null, message.LastError);
                }
                else
                {
                    message.DequeueCount++;
                    context.MoveMessageToEnd(message.ToJson());
                    context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
                }
                return;
            }

            await _transactionBlobStorage.AddOrReplaceTransaction(message.TransactionId, TransactionBlobType.Initial, transactionResponse.Transaction);


            await _queueFactory(Constants.ClientSignMonitoringQueue).PutRawMessageAsync(new WaitClientSignatureMessage
            {
                TransactionId = message.TransactionId,
                PutDateTime = DateTime.UtcNow
            }.ToJson());

            //TODO: uncomment for client signatures
            //try
            //{
            //    await _queueFactory(Constants.TransactionsForClientSignatureQueue).PutRawMessageAsync(new
            //    {
            //        TransactionId = message.TransactionId,
            //        Transaction = transactionResponse.Transaction
            //    }.ToJson());
            //}
            //catch (Exception ex)
            //{
            //    await _logger.WriteErrorAsync("TransactionBuildFunction", "ProcessMessage", message.ToJson(), ex);
            //}
        }
    }
}
