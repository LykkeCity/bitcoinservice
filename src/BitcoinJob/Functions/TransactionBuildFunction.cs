﻿using System;
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
using Lykke.Bitcoin.Contracts;
using Lykke.Bitcoin.Contracts.Events;
using Lykke.Cqrs;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;

namespace BitcoinJob.Functions
{
    public class TransactionBuildFunction
    {
        private readonly ILykkeTransactionBuilderService _lykkeTransactionBuilderService;
        private readonly IAssetRepository _assetRepository;
        private readonly Func<string, IQueueExt> _queueFactory;
        private readonly BaseSettings _settings;
        private readonly ILog _logger;
        private readonly ITransactionBlobStorage _transactionBlobStorage;
        private readonly ITransactionSignRequestRepository _signRequestRepository;
        private readonly ICqrsEngine _cqrsEngine;

        public TransactionBuildFunction(ILykkeTransactionBuilderService lykkeTransactionBuilderService,
            IAssetRepository assetRepository,
            Func<string, IQueueExt> queueFactory, BaseSettings settings, ILog logger, ITransactionBlobStorage transactionBlobStorage,
            ITransactionSignRequestRepository signRequestRepository, ICqrsEngine cqrsEngine)
        {
            _lykkeTransactionBuilderService = lykkeTransactionBuilderService;
            _assetRepository = assetRepository;
            _queueFactory = queueFactory;
            _settings = settings;
            _logger = logger;
            _transactionBlobStorage = transactionBlobStorage;
            _signRequestRepository = signRequestRepository;
            _cqrsEngine = cqrsEngine;
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
                    return;
                }

                switch (message.Type)
                {
                    case TransactionCommandType.Issue:
                        var issue = message.Command.DeserializeJson<IssueCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetIssueTransaction(
                            OpenAssetsHelper.ParseAddress(issue.Address),
                            issue.Amount, await _assetRepository.GetAssetById(issue.Asset), message.TransactionId);
                        break;
                    case TransactionCommandType.Transfer:
                        var transfer = message.Command.DeserializeJson<TransferCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetTransferTransaction(
                            OpenAssetsHelper.ParseAddress(transfer.SourceAddress),
                            OpenAssetsHelper.ParseAddress(transfer.DestinationAddress), transfer.Amount,
                            await _assetRepository.GetAssetById(transfer.Asset), message.TransactionId);
                        break;
                    case TransactionCommandType.TransferAll:
                        var transferAll = message.Command.DeserializeJson<TransferAllCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetTransferAllTransaction(
                            OpenAssetsHelper.ParseAddress(transferAll.SourceAddress),
                            OpenAssetsHelper.ParseAddress(transferAll.DestinationAddress),
                            message.TransactionId);
                        break;
                    case TransactionCommandType.Swap:
                        var swap = message.Command.DeserializeJson<SwapCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetSwapTransaction(
                            OpenAssetsHelper.ParseAddress(swap.MultisigCustomer1),
                            swap.Amount1,
                            await _assetRepository.GetAssetById(swap.Asset1),
                            OpenAssetsHelper.ParseAddress(swap.MultisigCustomer2),
                            swap.Amount2,
                            await _assetRepository.GetAssetById(swap.Asset2),
                            message.TransactionId);
                        break;
                    case TransactionCommandType.Destroy:
                        var destroy = message.Command.DeserializeJson<DestroyCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetDestroyTransaction(
                            OpenAssetsHelper.ParseAddress(destroy.Address),
                            destroy.Amount,
                            await _assetRepository.GetAssetById(destroy.Asset),
                            message.TransactionId);
                        break;
                    case TransactionCommandType.SegwitTransferToHotwallet:
                        var segwitTransfer = message.Command.DeserializeJson<SegwitTransferCommand>();
                        transactionResponse = await _lykkeTransactionBuilderService.GetTransferFromSegwitWallet(
                            OpenAssetsHelper.ParseAddress(segwitTransfer.SourceAddress), message.TransactionId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (BackendException e) when (e.Code == ErrorCode.NoCoinsFound)
            {
                if (message.Type == TransactionCommandType.SegwitTransferToHotwallet)
                {
                    _cqrsEngine.PublishEvent(new CashinCompletedEvent { OperationId = message.TransactionId }, BitcoinBoundedContext.Name);
                }
                return;
            }
            catch (BackendException e)
            {               
                if (e.Text != message.LastError)
                    await _logger.WriteWarningAsync("TransactionBuildFunction", "ProcessMessage", $"Id: [{message.TransactionId}], cmd: [{message.Command}]", e.Text);

                message.LastError = e.Text;
                if (message.DequeueCount >= _settings.MaxDequeueCount)
                {
                    context.MoveMessageToPoison(message.ToJson());
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


            await _queueFactory(Constants.BroadcastingQueue).PutRawMessageAsync(new BroadcastingTransaction
            {
                TransactionCommandType = message.Type,
                TransactionId = message.TransactionId
            }.ToJson());
        }
    }
}
