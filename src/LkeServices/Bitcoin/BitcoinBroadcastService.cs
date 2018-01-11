using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Core.Bitcoin;
using Core.Performance;
using Core.Providers;
using Core.Repositories.PaidFees;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using Core.TransactionMonitoring;
using NBitcoin;
using NBitcoin.RPC;
using BaseSettings = Core.Settings.BaseSettings;

namespace LkeServices.Bitcoin
{
    public class BitcoinBroadcastService : IBitcoinBroadcastService
    {
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly IRpcBitcoinClient _rpcBitcoinClient;
        private readonly ILykkeApiProvider _apiProvider;
        private readonly BaseSettings _settings;
        private readonly ITransactionMonitoringWriter _monitoringWriter;
        private readonly ILog _logger;
        private readonly IPaidFeesTaskWriter _paidFeesTaskWriter;

        public BitcoinBroadcastService(IBroadcastedOutputRepository broadcastedOutputRepository,
            IRpcBitcoinClient rpcBitcoinClient, ILykkeApiProvider apiProvider, BaseSettings settings,
            ITransactionMonitoringWriter monitoringWriter, ILog logger,
            IPaidFeesTaskWriter paidFeesTaskWriter)
        {
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _rpcBitcoinClient = rpcBitcoinClient;
            _apiProvider = apiProvider;
            _settings = settings;
            _monitoringWriter = monitoringWriter;
            _logger = logger;
            _paidFeesTaskWriter = paidFeesTaskWriter;
        }

        public async Task BroadcastTransaction(Guid transactionId, Transaction tx, IPerformanceMonitor monitor = null, bool useHandlers = true, Guid? notifyTxId = null)
        {
            var hash = tx.GetHash().ToString();

            if (_settings.UseLykkeApi && useHandlers)
            {
                monitor?.Step("Send prebroadcast notification");
                await _apiProvider.SendPreBroadcastNotification(new LykkeTransactionNotification(notifyTxId ?? transactionId, hash));
            }

            monitor?.Step("Broadcast transaction");
            try
            {
                await _rpcBitcoinClient.BroadcastTransaction(tx, transactionId);
            }
            catch (RPCException ex)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"[{transactionId}], ");
                builder.AppendLine(ex.Message + ":");
                foreach (var input in tx.Inputs)
                    builder.AppendLine(input.PrevOut.ToString());
                await _logger.WriteWarningAsync(nameof(BitcoinBroadcastService), nameof(BroadcastTransaction), builder.ToString(), ex);
                throw;
            }
            monitor?.Step("Set transaction hash and add to monitoring");
            await Task.WhenAll(
                _paidFeesTaskWriter.AddTask(hash, DateTime.UtcNow, null, null),
                _broadcastedOutputRepository.SetTransactionHash(transactionId, hash),
                _monitoringWriter.AddToMonitoring(transactionId, hash)
            );
        }
    }
}
