using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Bitcoin;
using Core.Providers;
using Core.Settings;
using LkeServices.Providers;
using LkeServices.Transactions;
using LkeServices.Triggers.Attributes;
using NBitcoin;

namespace BackgroundWorker.Functions
{
    public class SendFromChangeToHotWalletFunction
    {
        private const int SizeOfInputInBytes = 150;
        private const int InputsCount = 200;

        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly IFeeProvider _feeProvider;
        private readonly IBitcoinBroadcastService _bitcoinBroadcastService;
        private readonly ILykkeTransactionBuilderService _lykkeTransactionBuilderService;
        private readonly ISignatureApiProvider _signatureApiProvider;
        private readonly ILog _log;
        private readonly BaseSettings _baseSettings;

        public SendFromChangeToHotWalletFunction(IBitcoinOutputsService bitcoinOutputsService,
            IFeeProvider feeProvider,
            IBitcoinBroadcastService bitcoinBroadcastService,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            ILykkeTransactionBuilderService lykkeTransactionBuilderService,
            ILog log,
            BaseSettings baseSettings)
        {
            _bitcoinOutputsService = bitcoinOutputsService;
            _feeProvider = feeProvider;
            _bitcoinBroadcastService = bitcoinBroadcastService;
            _lykkeTransactionBuilderService = lykkeTransactionBuilderService;
            _signatureApiProvider = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
            _log = log;
            _baseSettings = baseSettings;
        }

        [TimerTrigger("24:00:00")]
        public async Task Send()
        {
            var feePerByte = (await _feeProvider.GetFeeRate()).FeePerK.Satoshi * _baseSettings.SpendChangeFeeRateMultiplier / 1000;

            var coins = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(_baseSettings.ChangeAddress)).OfType<Coin>()
                .Where(o => o.Amount.Satoshi > feePerByte * SizeOfInputInBytes).ToList();

            Utils.Shuffle(coins, new Random());

            while (coins.Count > InputsCount)
            {
                var part = coins.Take(InputsCount).ToList();
                var balance = part.Sum(o => o.Amount);

                var builder = new TransactionBuilder();
                builder.AddCoins(part);
                builder.Send(new BitcoinPubKeyAddress(_baseSettings.HotWalletForPregeneratedOutputs), balance);

                var tr = builder.BuildTransaction(false);
                var fee = new Money((await _feeProvider.CalcFeeForTransaction(builder)).Satoshi * _baseSettings.SpendChangeFeeRateMultiplier, MoneyUnit.Satoshi);
                if (fee > balance)
                {
                    await _log.WriteWarningAsync("SendFromChangeToHotWalletFunction", "Send", null,
                        $"Calculated fee is more than balance ({fee.Satoshi}>{balance})");
                    continue;
                }
                tr.Outputs[0].Value = tr.Outputs[0].Value - fee;

                var signed = await _signatureApiProvider.SignTransaction(tr.ToHex());
                var signedTr = new Transaction(signed);

                var transactionId = Guid.NewGuid();
                await _bitcoinBroadcastService.BroadcastTransaction(transactionId, signedTr);
                await _lykkeTransactionBuilderService.SaveSpentOutputs(transactionId, signedTr);

                coins = coins.Skip(InputsCount).ToList();
            }
        }
    }
}
