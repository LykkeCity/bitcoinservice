using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;
using Core.Notifiers;
using Core.QBitNinja;
using Core.Settings;
using Core.TransactionMonitoring;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using QBitNinja.Client.Models;

namespace BitcoinJob.Functions
{
    public class BroadcastMonitoringFunction
    {
        private readonly IQBitNinjaApiCaller _qBitNinjaApiCaller;
        private readonly ILog _logger;
        private readonly ISlackNotifier _slackNotifier;
        private readonly IFailedTransactionsManager _failedTransactionManager;
        private readonly BaseSettings _settings;

        public BroadcastMonitoringFunction(IQBitNinjaApiCaller qBitNinjaApiCaller, ILog logger,
            ISlackNotifier slackNotifier, IFailedTransactionsManager failedTransactionManager,
            BaseSettings settings)
        {
            _qBitNinjaApiCaller = qBitNinjaApiCaller;
            _logger = logger;
            _slackNotifier = slackNotifier;
            _failedTransactionManager = failedTransactionManager;
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
                await _failedTransactionManager.InsertFailedTransaction(message.TransactionId, message.TransactionHash, message.LastError);
            }
            else
            {
                context.MoveMessageToEnd(message.ToJson());
                context.SetCountQueueBasedDelay(10000, 100);
            }
        }

    }
}
