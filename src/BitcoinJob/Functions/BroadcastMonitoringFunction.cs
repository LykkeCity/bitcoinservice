using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;
using Core.Notifiers;
using Core.QBitNinja;
using Core.Settings;
using Core.TransactionMonitoring;
using LkeServices.Triggers.Attributes;
using LkeServices.Triggers.Bindings;
using QBitNinja.Client.Models;

namespace BackgroundWorker.Functions
{
    public class BroadcastMonitoringFunction
    {
        private readonly IQBitNinjaApiCaller _qBitNinjaApiCaller;
        private readonly ILog _logger;
        private readonly ISlackNotifier _slackNotifier;
        private readonly IFailedTransactionRepository _failedTransactionRepository;
        private readonly BaseSettings _settings;

        public BroadcastMonitoringFunction(IQBitNinjaApiCaller qBitNinjaApiCaller, ILog logger,
            ISlackNotifier slackNotifier, IFailedTransactionRepository failedTransactionRepository,
            BaseSettings settings)
        {
            _qBitNinjaApiCaller = qBitNinjaApiCaller;
            _logger = logger;
            _slackNotifier = slackNotifier;
            _failedTransactionRepository = failedTransactionRepository;
            _settings = settings;
        }

        [QueueTrigger(Constants.BroadcastMonitoringQueue)]
        public async Task Monitor(TransactionMonitoringMessage message, QueueTriggeringContext context)
        {
            try
            {
                var response = await _qBitNinjaApiCaller.GetTransaction(message.TransactionHash);
                if (response?.Block?.Confirmations > 0)
                    return;
            }
            catch (QBitNinjaException ex)
            {
                if (ex.Message != message.LastError)
                    await _logger.WriteWarningAsync("BroadcastMonitoringFunction", "Monitor",
                            $"TransactionHash: {message.TransactionHash}", $"Message: {ex.Message} StatusCode:{ex.StatusCode}");
                message.LastError = ex.Message;
            }
            if (DateTime.UtcNow - message.PutDateTime > TimeSpan.FromSeconds(_settings.BroadcastMonitoringPeriodSeconds))
            {
                context.MoveMessageToPoison(message.ToJson());
                await _slackNotifier.ErrorAsync($"Transaction with hash {message.TransactionHash} has no confirmations");
                await _failedTransactionRepository.AddFailedTransaction(message.TransactionId, message.TransactionHash);
            }
            else
            {
                context.MoveMessageToEnd(message.ToJson());
                context.SetCountQueueBasedDelay(10000, 100);
            }
        }

    }
}
