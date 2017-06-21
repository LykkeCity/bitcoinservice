using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Outputs;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using Lykke.JobTriggers.Triggers.Attributes;

namespace BitcoinJob.Functions
{
    public class RemoveOldSpentOutputsFunction
    {
        private readonly ISpentOutputRepository _spentOutputRepository;
        private readonly BaseSettings _settings;

        public RemoveOldSpentOutputsFunction(ISpentOutputRepository spentOutputRepository, BaseSettings settings)
        {            
            _spentOutputRepository = spentOutputRepository;
            _settings = settings;
        }

        [TimerTrigger("01:30:00")]
        public async Task Clean()
        {
            var bound = DateTime.UtcNow.AddDays(-_settings.SpentOutputsExpirationDays);
            do
            {
                var outputs = await _spentOutputRepository.GetOldSpentOutputs(bound, 10);
                if (!outputs.Any())
                    return;
                await _spentOutputRepository.RemoveSpentOutputs(outputs);
                await Task.Delay(500);
            } while (true);
        }
    }
}
