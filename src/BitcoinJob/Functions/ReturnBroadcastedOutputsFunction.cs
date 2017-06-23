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
using Lykke.JobTriggers.Triggers.Attributes;
using NBitcoin;

namespace BitcoinJob.Functions
{
    public class ReturnBroadcastedOutputsFunction
    {
        private readonly RpcConnectionParams _connectionParams;
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;

        public ReturnBroadcastedOutputsFunction(
            RpcConnectionParams connectionParams,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            IBitcoinOutputsService bitcoinOutputsService)
        {            
            _connectionParams = connectionParams;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _bitcoinOutputsService = bitcoinOutputsService;
        }

        [QueueTrigger(Constants.ReturnBroadcatedOutputsQueue)]
        public async Task Work(ReturnBroadcastedOutputMessage message)
        {
            var transaction = new Transaction(message.TransactionHex);
            var unspentOutputs = (await _bitcoinOutputsService.GetOnlyNinjaOutputs(message.Address, 0, 0)).ToList();
            foreach (var input in transaction.Inputs)
            {
                var coin = unspentOutputs.FirstOrDefault(o => o.Outpoint == input.PrevOut);
                if (coin != null)
                {
                    if (!await _broadcastedOutputRepository.OutputExists(coin.Outpoint.Hash.ToString(), (int)coin.Outpoint.N))
                        await _broadcastedOutputRepository.InsertOutputs(new List<IBroadcastedOutput>()
                            {
                                new BroadcastedOutput(coin, coin.Outpoint.Hash.ToString(), _connectionParams.Network)
                            });
                }
            }
        }
    }
}
