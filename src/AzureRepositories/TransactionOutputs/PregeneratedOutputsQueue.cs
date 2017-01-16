using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Core.Exceptions;
using Core.Helpers;
using Core.Repositories.TransactionOutputs;
using NBitcoin;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;

namespace AzureRepositories.TransactionOutputs
{
    public class PregeneratedOutputsQueue : IPregeneratedOutputsQueue
    {
        private readonly IQueueExt _queue;
        private readonly string _queueName;

        public PregeneratedOutputsQueue(IQueueExt queue, string queueName)
        {
            _queue = queue;
            _queueName = queueName;
        }

        public async Task<Coin> DequeueCoin()
        {
            var msg = await _queue.GetRawMessageAsync();
            if (msg == null)
                throw new BackendException($"Pregenerated pool '{_queueName}' is empty!", ErrorCode.PregeneratedPoolIsEmpty);

            await _queue.FinishRawMessageAsync(msg);
            return msg.AsString.DeserializeJson<SerializableCoin>().ToCoin();
        }

        public async Task EnqueueOutputs(params Coin[] coins)
        {
            if (coins == null)
                return;
            foreach (var item in coins)
            {
                await _queue.PutRawMessageAsync(new SerializableCoin(item).ToJson());
            }
        }

        public async Task<int> Count()
        {
            return await _queue.Count() ?? 0;
        }

        private class SerializableCoin
        {
            public string TransactionHash { get; set; }

            public uint N { get; set; }

            public string ScriptPubKey { get; set; }

            public long Amount { get; set; }


            public SerializableCoin()
            {

            }
            public SerializableCoin(Coin coin)
            {
                TransactionHash = coin.Outpoint.Hash.ToString();
                N = coin.Outpoint.N;
                ScriptPubKey = coin.ScriptPubKey.ToHex();
                Amount = coin.Amount.Satoshi;
            }

            public Coin ToCoin()
            {
                return new Coin(new OutPoint(uint256.Parse(TransactionHash), N), new TxOut(Amount, ScriptPubKey.ToScript()));
            }
        }
    }
}
