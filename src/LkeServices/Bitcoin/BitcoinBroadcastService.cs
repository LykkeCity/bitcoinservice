using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Providers;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using NBitcoin;

namespace LkeServices.Bitcoin
{
    public class BitcoinBroadcastService : IBitcoinBroadcastService
    {
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly IRpcBitcoinClient _rpcBitcoinClient;
        private readonly ILykkeApiProvider _apiProvider;
        private readonly BaseSettings _settings;

        public BitcoinBroadcastService(IBroadcastedOutputRepository broadcastedOutputRepository, IRpcBitcoinClient rpcBitcoinClient, ILykkeApiProvider apiProvider, BaseSettings settings)
        {
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _rpcBitcoinClient = rpcBitcoinClient;
            _apiProvider = apiProvider;
            _settings = settings;
        }

        public async Task BroadcastTransaction(Guid transactionId, Transaction tx)
        {
            var hash = tx.GetHash().ToString();

            if (_settings.UseLykkeApi)
                await _apiProvider.SendPreBroadcastNotification(transactionId, hash);

            await _rpcBitcoinClient.BroadcastTransaction(tx);

            await _broadcastedOutputRepository.SetTransactionHash(transactionId, hash);

            if (_settings.UseLykkeApi)
                await _apiProvider.SendPostBroadcastNotification(transactionId, hash);
        }
    }
}
