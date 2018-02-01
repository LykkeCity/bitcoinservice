using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Bitcoin;
using Core.Notifiers;
using Core.Providers;
using Core.Repositories.MultipleCashouts;
using Core.Repositories.Settings;
using LkeServices.Providers;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using NBitcoin;
using NBitcoin.RPC;

namespace BitcoinJob.Functions
{
    public class MultipleCashoutFunction
    {
        private const int MaxCountAggregatedCashoutsDefault = 50;
        private const int MaxCashoutDelaySecondsDefault = 30;
        private const int MaxMultiCashoutTryCount = 10;

        private readonly ICashoutRequestRepository _cashoutRequestRepository;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IMultiCashoutRepository _multiCashoutRepository;
        private readonly ISlackNotifier _slackNotifier;
        private readonly IBitcoinTransactionService _bitcoinTransactionService;
        private readonly ILykkeTransactionBuilderService _lykkeTransactionBuilderService;
        private readonly IBitcoinBroadcastService _bitcoinBroadcastService;
        private readonly ISignatureApiProvider _signatureApi;

        public MultipleCashoutFunction(ICashoutRequestRepository cashoutRequestRepository,
            ISettingsRepository settingsRepository, IMultiCashoutRepository multiCashoutRepository,
            ISlackNotifier slackNotifier, IBitcoinTransactionService bitcoinTransactionService,
            ISignatureApiProvider signatureApiProvider,
            ILykkeTransactionBuilderService lykkeTransactionBuilderService, IBitcoinBroadcastService bitcoinBroadcastService)
        {
            _cashoutRequestRepository = cashoutRequestRepository;
            _settingsRepository = settingsRepository;
            _multiCashoutRepository = multiCashoutRepository;
            _slackNotifier = slackNotifier;
            _bitcoinTransactionService = bitcoinTransactionService;
            _signatureApi = signatureApiProvider;
            _lykkeTransactionBuilderService = lykkeTransactionBuilderService;
            _bitcoinBroadcastService = bitcoinBroadcastService;
        }

        [TimerTrigger("00:00:10")]
        public async Task Cashout()
        {
            var currentMultiCashout = await _multiCashoutRepository.GetCurrentMultiCashout();
            if (currentMultiCashout != null)
            {
                Transaction tx = null;
                try
                {
                    tx = await _bitcoinTransactionService.GetTransaction(currentMultiCashout.TransactionHash);
                }
                catch
                {
                }
                if (tx != null)
                    await _multiCashoutRepository.CompleteMultiCashout(currentMultiCashout.MultipleCashoutId);
                else
                    await RetryBroadcast(currentMultiCashout);
            }

            var cashouts = (await _cashoutRequestRepository.GetOpenRequests()).OrderBy(o => o.Date).ToList();
            if (!cashouts.Any())
                return;
            var maxCount = await _settingsRepository.Get(Constants.MaxCountAggregatedCashouts, MaxCountAggregatedCashoutsDefault);
            var cashoutDelay = TimeSpan.FromSeconds(await _settingsRepository.Get(Constants.MaxCashoutDelaySeconds, MaxCashoutDelaySecondsDefault));

            cashouts = cashouts.Take(maxCount).ToList();

            if (cashouts.Count < maxCount && DateTime.UtcNow - cashouts.First().Date < cashoutDelay)
                return;

            var multiCashoutId = Guid.NewGuid();
            var createTxData = await _lykkeTransactionBuilderService.GetMultipleCashoutTransaction(cashouts.ToList(), multiCashoutId);

            var signedHex = await _signatureApi.SignTransaction(createTxData.Transaction.ToHex());

            var signedTx = new Transaction(signedHex);

            await _cashoutRequestRepository.SetMultiCashoutId(createTxData.UsedRequests.Select(o => o.CashoutRequestId), multiCashoutId);

            await _multiCashoutRepository.CreateMultiCashout(multiCashoutId, signedHex, signedTx.GetHash().ToString());

            await _bitcoinBroadcastService.BroadcastTransaction(multiCashoutId, createTxData.UsedRequests.Select(o => o.CashoutRequestId).ToList(),
                signedTx);

            await _multiCashoutRepository.CompleteMultiCashout(multiCashoutId);
        }

        private async Task RetryBroadcast(IMultipleCashout currentMultiCashout)
        {
            if (currentMultiCashout.TryCount > MaxMultiCashoutTryCount)
            {
                await CloseMultiCashout(currentMultiCashout);
                return;
            }
            await _multiCashoutRepository.IncreaseTryCount(currentMultiCashout.MultipleCashoutId);
            
            var cashouts = await _cashoutRequestRepository.GetCashoutRequests(currentMultiCashout.MultipleCashoutId);

            await _bitcoinBroadcastService.BroadcastTransaction(currentMultiCashout.MultipleCashoutId, cashouts.Select(o => o.CashoutRequestId).ToList(),
                new Transaction(currentMultiCashout.TransactionHex));

            await _multiCashoutRepository.CompleteMultiCashout(currentMultiCashout.MultipleCashoutId);
        }

        private async Task CloseMultiCashout(IMultipleCashout currentMultiCashout)
        {
            await _multiCashoutRepository.CloseMultiCashout(currentMultiCashout.MultipleCashoutId);
            await _slackNotifier.ErrorAsync($"Bitcoin: can't broadcast multicashout {currentMultiCashout.MultipleCashoutId}");
        }
    }
}
