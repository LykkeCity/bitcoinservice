using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
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

        public async Task BroadcastTransaction(Guid transactionId, Transaction tx)
        {
            var hash = tx.GetHash().ToString();

            if (_settings.UseLykkeApi)
                await _apiProvider.SendPreBroadcastNotification(new LykkeTransactionNotification(transactionId, hash));

            await _rpcBitcoinClient.BroadcastTransaction(tx, transactionId);

            await _broadcastedOutputRepository.SetTransactionHash(transactionId, hash);

            if (_settings.UseLykkeApi)
                await _apiProvider.SendPostBroadcastNotification(new LykkeTransactionNotification(transactionId, hash));

            await _monitoringWriter.AddToMonitoring(transactionId, tx.GetHash().ToString());
        }
    }
}
