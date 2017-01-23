using System;
using System.Threading.Tasks;
using AzureRepositories.TransactionMonitoring;
using AzureStorage.Queue;
using Common;
using Common.Log;
using Core;
using Core.Exceptions;
using Core.Providers;
using Core.Repositories.Assets;
using Core.Repositories.TransactionSign;
using Core.Settings;
using Core.TransactionMonitoring;
using Core.TransactionQueueWriter;
using Core.TransactionQueueWriter.Commands;
using LkeServices.Providers;
using LkeServices.Transactions;
using LkeServices.Triggers.Attributes;
using LkeServices.Triggers.Bindings;
using static Core.OpenAssets.OpenAssetsHelper;
namespace BackgroundWorker.Functions
{
    public class TransactionBuildFunction
    {
        private readonly ILykkeTransactionBuilderService _lykkeTransactionBuilderService;
        private readonly IAssetRepository _assetRepository;
        private readonly IFailedTransactionsManager _failedTransactionManager;
        private readonly Func<string, IQueueExt> _queueFactory;
        private readonly BaseSettings _settings;
        private readonly ISignatureApiProvider _clientSignatureApi;
        private readonly ISignatureApiProvider _exchangeSignatureApi;
        private readonly ILog _logger;
        private readonly ITransactionSignRequestRepository _signRequestRepository;

        public TransactionBuildFunction(ILykkeTransactionBuilderService lykkeTransactionBuilderService,
            IAssetRepository assetRepository, Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            IFailedTransactionsManager failedTransactionManager,
            Func<string, IQueueExt> queueFactory, BaseSettings settings, ILog logger, ITransactionSignRequestRepository signRequestRepository)
        {
            _lykkeTransactionBuilderService = lykkeTransactionBuilderService;
            _assetRepository = assetRepository;
            _failedTransactionManager = failedTransactionManager;
            _queueFactory = queueFactory;
            _settings = settings;
            _logger = logger;
            _signRequestRepository = signRequestRepository;

            _clientSignatureApi = signatureApiProviderFactory(SignatureApiProviderType.Client);
            _exchangeSignatureApi = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
        }


        [QueueTrigger(Constants.TransactionCommandQueue, 100, true)]
        public async Task ProcessMessage(TransactionQueueMessage message, QueueTriggeringContext context)
        {
            CreateTransactionResponse transactionResponse;
            try
            {
                switch (message.Type)
                {
                    case TransactionCommandType.Issue:
                        var issue = message.Command.DeserializeJson<IssueCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetIssueTransaction(GetBitcoinAddressFormBase58Date(issue.Address),
                                                        issue.Amount, await _assetRepository.GetAssetById(issue.Asset), message.TransactionId);
                        break;
                    case TransactionCommandType.Transfer:
                        var transfer = message.Command.DeserializeJson<TransferCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetTransferTransaction(
                                                        GetBitcoinAddressFormBase58Date(transfer.SourceAddress),
                                                        GetBitcoinAddressFormBase58Date(transfer.DestinationAddress), transfer.Amount,
                                                        await _assetRepository.GetAssetById(transfer.Asset), message.TransactionId);
                        break;
                    case TransactionCommandType.TransferAll:
                        var transferAll = message.Command.DeserializeJson<TransferAllCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetTransferAllTransaction(
                                                        GetBitcoinAddressFormBase58Date(transferAll.SourceAddress),
                                                        GetBitcoinAddressFormBase58Date(transferAll.DestinationAddress),
                                                        message.TransactionId);
                        break;
                    case TransactionCommandType.Swap:
                        var swap = message.Command.DeserializeJson<SwapCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetSwapTransaction(
                                                        GetBitcoinAddressFormBase58Date(swap.MultisigCustomer1),
                                                        swap.Amount1,
                                                        await _assetRepository.GetAssetById(swap.Asset1),
                                                        GetBitcoinAddressFormBase58Date(swap.MultisigCustomer2),
                                                        swap.Amount2,
                                                        await _assetRepository.GetAssetById(swap.Asset2),
                                                        message.TransactionId);
                        break;
                    case TransactionCommandType.Destroy:
                        var destroy = message.Command.DeserializeJson<DestroyCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetDestroyTransaction(
                                                        GetBitcoinAddressFormBase58Date(destroy.Address),
                                                        destroy.Amount,
                                                        await _assetRepository.GetAssetById(destroy.Asset),
                                                        message.TransactionId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (BackendException e)
            {
                if (e.Text != message.LastError)
                    await _logger.WriteWarningAsync("TransactionBuildFunction", "ProcessMessage", $"Id: [{message.TransactionId}], cmd: [{message.Command}]", e.Text);

                message.LastError = e.Text;
                if (message.DequeueCount >= _settings.MaxDequeueCount)
                {
                    context.MoveMessageToPoison(message.ToJson());
                    await _failedTransactionManager.InsertFailedTransaction(message.TransactionId, null);
                }
                else
                {
                    message.DequeueCount++;
                    context.MoveMessageToEnd(message.ToJson());
                    context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
                }
                return;
            }

            var signedByClientTr = await _clientSignatureApi.SignTransaction(transactionResponse.Transaction);
            var signedByExchangeTr = await _exchangeSignatureApi.SignTransaction(signedByClientTr);

            await _signRequestRepository.InsertSignRequest(message.TransactionId, signedByExchangeTr, 0);

            await _queueFactory(Constants.BroadcastingQueue).PutRawMessageAsync(new BroadcastingTransaction
            {
                TransactionId = message.TransactionId,
                TransactionHex = signedByExchangeTr
            }.ToJson());
        }
    }
}
