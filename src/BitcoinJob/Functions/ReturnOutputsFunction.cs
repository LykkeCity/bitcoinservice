using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Bitcoin;
using Core.OpenAssets;
using Core.Outputs;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using Lykke.JobTriggers.Triggers.Attributes;
using NBitcoin;

namespace BitcoinJob.Functions
{
    public class ReturnOutputsFunction
    {
        private readonly RpcConnectionParams _connectionParams;
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly BaseSettings _settings;
        private readonly IPregeneratedOutputsQueue _pregeneratedQueue;

        public ReturnOutputsFunction(
            RpcConnectionParams connectionParams,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            IBitcoinOutputsService bitcoinOutputsService,
            IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory,
            BaseSettings settings)
        {
            _connectionParams = connectionParams;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _bitcoinOutputsService = bitcoinOutputsService;
            _settings = settings;
            _pregeneratedQueue = pregeneratedOutputsQueueFactory.CreateFeeQueue();
        }

        [QueueTrigger(Constants.ReturnBroadcatedOutputsQueue)]
        public async Task Work(ReturnOutputMessage message)
        {
            var transaction = new Transaction(message.TransactionHex);

            foreach (var address in message.Addresses)
            {
                var unspentOutputs = (await _bitcoinOutputsService.GetOnlyNinjaOutputs(address, 0)).ToList();
                foreach (var input in transaction.Inputs)
                {
                    var coin = unspentOutputs.FirstOrDefault(o => o.Outpoint == input.PrevOut);
                    if (coin != null)
                    {
                        if (address != _settings.FeeAddress)
                        {
                            if (!await _broadcastedOutputRepository.OutputExists(coin.Outpoint.Hash.ToString(), (int)coin.Outpoint.N))
                                await _broadcastedOutputRepository.InsertOutputs(new List<IBroadcastedOutput>()
                                {
                                    new BroadcastedOutput(coin, coin.Outpoint.Hash.ToString(), _connectionParams.Network)
                                });
                        }
                        else
                        {
                            if (coin is Coin uncolored)
                                await _pregeneratedQueue.EnqueueOutputs(uncolored);
                        }
                    }
                }
            }

        }
    }
}
