using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Exceptions;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using Lykke.JobTriggers.Triggers.Attributes;
using NBitcoin;

namespace BitcoinJob.Functions
{
    public class ReturnFeeOutputsFunction
    {
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly BaseSettings _settings;

        public ReturnFeeOutputsFunction(IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory,
            IBitcoinOutputsService bitcoinOutputsService, BaseSettings settings)
        {
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _bitcoinOutputsService = bitcoinOutputsService;
            _settings = settings;
        }

        [TimerTrigger("05:00:00")]
        public async Task Work()
        {
            var queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();
            var address = _settings.FeeAddress;

            var set = new HashSet<OutPoint>();
            int prevCount;
            var count = prevCount = await queue.Count();
            while (count-- > 0)
            {
                Coin coin = null;
                try
                {
                    coin = await queue.DequeueCoin();

                    set.Add(coin.Outpoint);
                }
                catch (BackendException)
                {
                    //ignore
                    break;
                }
                finally
                {
                    if (coin != null)
                        await queue.EnqueueOutputs(coin);
                }
            }

            var coins = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(address)).OfType<Coin>().ToArray();

            coins = coins.Where(x => !set.Contains(x.Outpoint)).ToArray();

            var newCount = await queue.Count();
            if (newCount > prevCount)
                throw new Exception("Queue length is greater than initial length");

            await queue.EnqueueOutputs(coins);
        }
    }
}
