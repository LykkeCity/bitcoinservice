using System.Threading.Tasks;
using Core.Notifiers;
using Core.Repositories.ExtraAmounts;
using Core.Settings;
using Lykke.JobTriggers.Triggers.Attributes;
using NBitcoin;

namespace BitcoinJob.Functions
{
    public class ExtraAmountMonitoringFunction
    {
        private readonly IExtraAmountRepository _extraAmountRepository;
        private readonly BaseSettings _baseSettings;
        private readonly ISlackNotifier _slackNotifier;

        public ExtraAmountMonitoringFunction(IExtraAmountRepository extraAmountRepository, BaseSettings baseSettings, ISlackNotifier slackNotifier)
        {
            _extraAmountRepository = extraAmountRepository;
            _baseSettings = baseSettings;
            _slackNotifier = slackNotifier;
        }

        [TimerTrigger("06:00:00")]
        public async Task Process()
        {
            var maxAmount = new Money(_baseSettings.MaxExtraAmount, MoneyUnit.BTC);
            var extraAmounts = await _extraAmountRepository.GetData();
            foreach (var extraAmount in extraAmounts)
            {
                if (extraAmount.Amount > maxAmount)
                {
                    await _slackNotifier.WarningAsync($"Address [{extraAmount.Address}] already has [{extraAmount.Amount}] extra satoshi!");
                }
            }
        }
    }
}
