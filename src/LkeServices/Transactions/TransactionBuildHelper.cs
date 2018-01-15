using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Bitcoin;
using Core.Exceptions;
using Core.OpenAssets;
using Core.Providers;
using Core.Repositories.ExtraAmounts;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using NBitcoin;
using NBitcoin.OpenAsset;
using NBitcoin.Policy;
using BaseSettings = Core.Settings.BaseSettings;
using RpcConnectionParams = Core.Settings.RpcConnectionParams;

namespace LkeServices.Transactions
{
    public interface ITransactionBuildHelper
    {
        Task<Money> AddFee(TransactionBuilder builder, TransactionBuildContext context, decimal? feeMultiplier = null);
        Task<decimal> SendWithChange(TransactionBuilder builder, TransactionBuildContext context, List<ICoin> coins, IDestination destination, Money amount, IDestination changeDestination, bool addDust = true);
        void SendAssetWithChange(TransactionBuilder builder, TransactionBuildContext context, List<ColoredCoin> coins, IDestination destination, AssetMoney amount, IDestination changeDestination);
        void AddFakeInput(TransactionBuilder builder, Money fakeAmount);
        void RemoveFakeInput(Transaction tr);
        void AggregateOutputs(Transaction tr);
        Task AddFee(Transaction tr, TransactionBuildContext context);
        Task AddFeeWithoutChange(Transaction tr, TransactionBuildContext context, int maxCoins = int.MaxValue);
        Task<Money> CalcFee(Transaction tr, int feeRate = 0);
        Task<Money> CalcFee(int inputsCount, int outputsCount);
    }

    public class TransactionBuildHelper : ITransactionBuildHelper
    {
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly BaseSettings _baseSettings;
        private readonly RpcConnectionParams _connectionParams;
        private readonly IFeeProvider _feeProvider;
        private readonly IExtraAmountRepository _extraAmountRepository;

        public TransactionBuildHelper(IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory, BaseSettings baseSettings,
            RpcConnectionParams connectionParams, IFeeProvider feeProvider, IExtraAmountRepository extraAmountRepository)
        {
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _baseSettings = baseSettings;
            _connectionParams = connectionParams;
            _feeProvider = feeProvider;
            _extraAmountRepository = extraAmountRepository;
        }

        public async Task<Money> AddFee(TransactionBuilder builder, TransactionBuildContext context, decimal? feeMultiplier = null)
        {
            builder.SetChange(BitcoinAddress.Create(_baseSettings.ChangeAddress, _connectionParams.Network), ChangeType.Uncolored);

            var totalFeeSent = Money.Zero;
            var sentAmount = Money.Zero;
            var dustAmount = Money.Zero;
            try
            {
                var precalculatedFee = await _feeProvider.CalcFeeForTransaction(builder, feeMultiplier);
                builder.SendFees(precalculatedFee);
                totalFeeSent = precalculatedFee;
            }
            catch (NotEnoughFundsException ex)
            {
                if (ex.Missing is Money)
                    dustAmount = ((Money)ex.Missing).Satoshi;
                else throw;
            }
            var queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();
            do
            {
                var feeInput = await queue.DequeueCoin();
                builder.AddCoins(feeInput);
                context.AddCoins(true, feeInput);
                sentAmount += feeInput.Amount;
                if (sentAmount < dustAmount + totalFeeSent)
                    continue;

                var newEstimate = await _feeProvider.CalcFeeForTransaction(builder, feeMultiplier);

                builder.SendFees(newEstimate - totalFeeSent);
                totalFeeSent = newEstimate;
            } while (totalFeeSent + dustAmount > sentAmount);

            builder.Send(BitcoinAddress.Create(_baseSettings.ChangeAddress, _connectionParams.Network), sentAmount - dustAmount - totalFeeSent);
            return totalFeeSent + dustAmount;
        }

        public async Task<decimal> SendWithChange(TransactionBuilder builder, TransactionBuildContext context, List<ICoin> coins, IDestination destination, Money amount, IDestination changeDestination, bool addDust = true)
        {
            if (amount.Satoshi <= 0)
                throw new BackendException("Amount can't be less or equal to zero", ErrorCode.BadInputParameter);

            Action throwError = () =>
            {
                throw new BackendException($"The sum of total applicable outputs is less than the required: {amount.Satoshi} satoshis.", ErrorCode.NotEnoughBitcoinAvailable);
            };

            var selectedCoins = OpenAssetsHelper.CoinSelect(coins, amount);
            if (selectedCoins == null)
                throwError();

            var orderedCoins = selectedCoins.OrderBy(o => o.Amount).ToList();
            var sendAmount = Money.Zero;
            var cnt = 0;
            while (sendAmount < amount && cnt < orderedCoins.Count)
            {
                sendAmount += orderedCoins[cnt].TxOut.Value;
                cnt++;
            }
            if (sendAmount < amount)
                throwError();

            context.AddCoins(orderedCoins.Take(cnt));
            builder.AddCoins(orderedCoins.Take(cnt));

            var sent = await Send(builder, context, destination, amount, addDust);

            if (sendAmount - amount > 0)
                await Send(builder, context, changeDestination, sendAmount - amount, addDust);
            return sent;
        }

        private async Task<decimal> Send(TransactionBuilder builder, TransactionBuildContext context, IDestination destination, Money amount, bool addDust)
        {
            var newAmount = Money.Max(GetDust(destination, addDust), amount);
            builder.Send(destination, newAmount);
            if (newAmount > amount)
                context.AddExtraAmount(await _extraAmountRepository.Add(destination.ScriptPubKey.GetDestinationAddress(_connectionParams.Network).ToString(),
                            newAmount - amount));
            return newAmount.ToDecimal(MoneyUnit.BTC);
        }


        private Money GetDust(IDestination destination, bool addDust = true)
        {
            return addDust ? new TxOut(Money.Zero, destination.ScriptPubKey).GetDustThreshold(new StandardTransactionPolicy().MinRelayTxFee) : Money.Zero;
        }

        public void SendAssetWithChange(TransactionBuilder builder, TransactionBuildContext context, List<ColoredCoin> coins, IDestination destination, AssetMoney amount,
            IDestination changeDestination)
        {
            if (amount.Quantity <= 0)
                throw new BackendException("Amount can't be less or equal to zero", ErrorCode.BadInputParameter);

            Action throwError = () =>
            {
                throw new BackendException($"The sum of total applicable outputs is less than the required: {amount.Quantity} {amount.Id}.", ErrorCode.NotEnoughAssetAvailable);
            };

            var selectedCoins = OpenAssetsHelper.CoinSelect(coins, amount);
            if (selectedCoins == null)
                throwError();

            var orderedCounts = selectedCoins.Cast<ColoredCoin>().OrderBy(o => o.Amount).ToList();
            var sendAmount = new AssetMoney(amount.Id);
            var cnt = 0;
            while (sendAmount < amount && cnt < orderedCounts.Count)
            {
                sendAmount += orderedCounts[cnt].Amount;
                cnt++;
            }

            if (sendAmount < amount)
                throwError();

            builder.AddCoins(orderedCounts.Take(cnt));
            context.AddCoins(orderedCounts.Take(cnt));
            builder.SendAsset(destination, amount);

            if ((sendAmount - amount).Quantity > 0)
                builder.SendAsset(changeDestination, sendAmount - amount);
        }

        public void AddFakeInput(TransactionBuilder builder, Money fakeAmount)
        {
            var coin = new Coin(
                new OutPoint(uint256.One, 0),
                new TxOut { ScriptPubKey = new Key().ScriptPubKey, Value = fakeAmount });
            builder.AddCoins(coin);
        }

        public void RemoveFakeInput(Transaction tr)
        {
            var input = tr.Inputs.FirstOrDefault(o => o.PrevOut.Hash == uint256.One);
            if (input != null)
            {
                tr.Inputs.Remove(input);
            }
        }

        public void AggregateOutputs(Transaction tr)
        {

            var finalOutputs = new Dictionary<string, TxOut>();

            var marker = ColorMarker.Get(tr);
            var quantites = new Dictionary<string, ulong>();

            foreach (var trOutput in tr.Outputs.AsIndexedOutputs().Skip(marker == null ? 0 : 1))
            {
                var key = trOutput.TxOut.ScriptPubKey.ToHex();

                if (finalOutputs.ContainsKey(key))
                {
                    finalOutputs[key].Value += trOutput.TxOut.Value;
                    if (marker != null && marker.Quantities.Length >= trOutput.N)
                        quantites[key] += marker.Quantities[(int)trOutput.N - 1];

                }
                else
                {
                    finalOutputs[key] = trOutput.TxOut;
                    if (marker != null && marker.Quantities.Length >= trOutput.N)
                        quantites[key] = marker.Quantities[(int)trOutput.N - 1];
                }
            }
            tr.Outputs.Clear();

            var outputs = finalOutputs.ToList();
            if (marker != null)
            {
                var newMarker = new ColorMarker();
                newMarker.Quantities = outputs.Select(o => quantites.ContainsKey(o.Key) ? quantites[o.Key] : 0).ToArray();
                tr.Outputs.Add(new TxOut()
                {
                    ScriptPubKey = newMarker.GetScript(),
                    Value = Money.Zero
                });
            }
            tr.Outputs.AddRange(outputs.Select(o => o.Value));
        }

        public async Task AddFee(Transaction tr, TransactionBuildContext context)
        {
            var fee = await _feeProvider.CalcFeeForTransaction(tr);
            var providedAmount = Money.Zero;
            var queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();
            do
            {
                var feeInput = await queue.DequeueCoin();
                context.AddCoins(true, feeInput);
                tr.Inputs.Add(new TxIn
                {
                    PrevOut = feeInput.Outpoint
                });
                providedAmount += feeInput.Amount;

                fee = await _feeProvider.CalcFeeForTransaction(tr);
                if (fee <= providedAmount)
                {
                    if (fee < providedAmount)
                        tr.Outputs.Add(new TxOut(providedAmount - fee, feeInput.ScriptPubKey));
                    return;
                }

            } while (true);
        }

        public async Task AddFeeWithoutChange(Transaction tr, TransactionBuildContext context, int maxCoins = int.MaxValue)
        {
            Money fee = Money.Zero;
            var providedAmount = Money.Zero;
            var queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();
            int count = 0;
            do
            {
                var feeInput = await queue.DequeueCoin();
                context.AddCoins(true, feeInput);
                count++;
                tr.Inputs.Add(new TxIn
                {
                    PrevOut = feeInput.Outpoint
                });
                providedAmount += feeInput.Amount;
                fee = await _feeProvider.CalcFeeForTransaction(tr);
            } while (fee > providedAmount && count < maxCoins);
        }

        public Task<Money> CalcFee(Transaction tr, int feeRate = 0)
        {
            return _feeProvider.CalcFeeForTransaction(tr, feeRate);
        }

        public Task<Money> CalcFee(int inputsCount, int outputsCount)
        {
            return _feeProvider.CalcFee(inputsCount * Constants.InputSize + outputsCount * 33 + 10);
        }
    }
}
