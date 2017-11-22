using System;
using System.Threading.Tasks;
using Core;
using Core.Notifiers;
using Core.Providers;
using Core.Repositories.Assets;
using Core.Repositories.FeeRate;
using Core.Repositories.Settings;
using Core.Settings;
using Lykke.JobTriggers.Triggers.Attributes;

namespace BitcoinJob.Functions
{
    public class FeeRateUpdateFunction
    {
        private readonly IFeeRateRepository _feeRateRepository;
        private readonly IFeeRateApiProvider _feerateApiProvider;
        private readonly BaseSettings _settings;
        private readonly ISettingsRepository _settingsRepository;
        private readonly ISlackNotifier _slackNotifier;
        private static DateTime _lastWarningSentTime = DateTime.MinValue;

        private const int DefaultMaxFeeRate = 500;

        public FeeRateUpdateFunction(IFeeRateRepository feeRateRepository, 
            IFeeRateApiProvider feerateApiProvider, 
            BaseSettings settings, ISettingsRepository settingsRepository, ISlackNotifier slackNotifier)
        {
            _feeRateRepository = feeRateRepository;
            _feerateApiProvider = feerateApiProvider;
            _settings = settings;
            _settingsRepository = settingsRepository;
            _slackNotifier = slackNotifier;
        }

        [TimerTrigger("1:00:00")]
        public async Task FeeRateUpdate()
        {
            FeeResult feeRate = null;
            try
            {
                feeRate = await _feerateApiProvider.GetFee();
            }
            catch (TaskCanceledException)
            {
                //ignored                
                return;
            }            
            int newFeeRate;
            switch (_settings.FeeType)
            {
                case Core.Enums.FeeType21co.FastestFee:
                    newFeeRate = feeRate.FastestFee;
                    break;
                case Core.Enums.FeeType21co.HalfHourFee:
                    newFeeRate = feeRate.HalfHourFee;
                    break;
                case Core.Enums.FeeType21co.HourFee:
                    newFeeRate = feeRate.HourFee;
                    break;
                default:
                    throw new Exception("unsupported fee type");
            }
            var maxFeeRate = await _settingsRepository.Get(Constants.MaxFeeRateSetting, DefaultMaxFeeRate);
            if (newFeeRate > maxFeeRate)
            {                
                if (DateTime.UtcNow - _lastWarningSentTime > TimeSpan.FromHours(1))
                {
                    await _slackNotifier.FinanceWarningAsync($"Current fee rate={newFeeRate} is more than max fee rate={maxFeeRate}");
                    _lastWarningSentTime = DateTime.UtcNow;
                }
                newFeeRate = maxFeeRate;
            }
            await _feeRateRepository.UpdateFeeRate(newFeeRate);
        }
    }
}
