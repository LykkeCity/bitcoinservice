using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Perfomance;
using Core.Providers;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using Core.TransactionMonitoring;
using NBitcoin;

namespace LkeServices.Bitcoin
{
    public class BitcoinBroadcastService : IBitcoinBroadcastService
    {
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly IRpcBitcoinClient _rpcBitcoinClient;
        private readonly ILykkeApiProvider _apiProvider;
        private readonly BaseSettings _settings;
        private readonly ITransactionMonitoringWriter _monitoringWriter;

        public BitcoinBroadcastService(IBroadcastedOutputRepository broadcastedOutputRepository,
            IRpcBitcoinClient rpcBitcoinClient, ILykkeApiProvider apiProvider, BaseSettings settings,
            ITransactionMonitoringWriter monitoringWriter)
        {
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _rpcBitcoinClient = rpcBitcoinClient;
            _apiProvider = apiProvider;
            _settings = settings;
            _monitoringWriter = monitoringWriter;
        }

        public async Task BroadcastTransaction(Guid transactionId, Transaction tx, IPerfomanceMonitor monitor)
        {
            var hash = tx.GetHash().ToString();

            if (_settings.UseLykkeApi)
            {
                monitor.Step("Send prebroadcast notification");
                await _apiProvider.SendPreBroadcastNotification(new LykkeTransactionNotification(transactionId, hash));
            }

            monitor.Step("Broadcast transcation");
            await _rpcBitcoinClient.BroadcastTransaction(tx, transactionId);
            
            monitor.Step("Set transaction hash, postbroadcast and add to monitoring");
            await Task.WhenAll(
                _broadcastedOutputRepository.SetTransactionHash(transactionId, hash),
                Task.Run(async () =>
                    {
                        if (_settings.UseLykkeApi)
                        {
                            await _apiProvider.SendPostBroadcastNotification(new LykkeTransactionNotification(transactionId, hash));
                        }
                    }
                ),
                _monitoringWriter.AddToMonitoring(transactionId, tx.GetHash().ToString())
            );
        }
    }
}
