using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using Lykke.JobTriggers.Triggers.Attributes;
using MongoDB.Driver;

namespace BitcoinJob.Functions
{
    public class RemoveOldBroadcatedOutputs
    {
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly BaseSettings _settings;

        public RemoveOldBroadcatedOutputs(IBroadcastedOutputRepository broadcastedOutputRepository, BaseSettings settings)
        {
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _settings = settings;
        }


        [TimerTrigger("01:00:00")]
        public async Task Clean()
        {
            var bound = DateTime.UtcNow.AddDays(-_settings.BroadcastedOutputsExpirationDays);
            do
            {
                var outputs = await _broadcastedOutputRepository.GetOldOutputs(bound, 10);
                if (!outputs.Any())
                    return;
                await _broadcastedOutputRepository.DeleteBroadcastedOutputs(outputs);
                await Task.Delay(500);
            } while (true);
        }
    }
}

