using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Exceptions;
using Core.OpenAssets;
using Core.Providers;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace LkeServices.Transactions
{
    public interface ITransactionBuildHelper
    {
        Task AddFee(TransactionBuilder builder, TransactionBuildContext context);
        void SendWithChange(TransactionBuilder builder, TransactionBuildContext context, List<ICoin> coins, IDestination destination, Money amount, IDestination changeDestination);
        void SendAssetWithChange(TransactionBuilder builder, TransactionBuildContext context, List<ColoredCoin> coins, IDestination destination, AssetMoney amount, IDestination changeDestination);
        void AddFakeInput(TransactionBuilder builder, Money fakeAmount);
        void RemoveFakeInput(Transaction tr);
        void AggregateOutputs(Transaction tr);
        Task AddFee(Transaction tr);
    }

    public class TransactionBuildHelper : ITransactionBuildHelper
    {
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly BaseSettings _baseSettings;
        private readonly RpcConnectionParams _connectionParams;
        private readonly IFeeProvider _feeProvider;

        public TransactionBuildHelper(IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory, BaseSettings baseSettings,
            RpcConnectionParams connectionParams, IFeeProvider feeProvider)
        {
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _baseSettings = baseSettings;
            _connectionParams = connectionParams;
            _feeProvider = feeProvider;
        }

        public async Task AddFee(TransactionBuilder builder, TransactionBuildContext context)
        {
            builder.SetChange(BitcoinAddress.Create(_baseSettings.ChangeAddress, _connectionParams.Network), ChangeType.Uncolored);

            var totalFeeSent = Money.Zero;
            var sentAmount = Money.Zero;
            var dustAmount = Money.Zero;
            try
            {
                var precalculatedFee = await _feeProvider.CalcFeeForTransaction(builder);
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

                var newEstimate = await _feeProvider.CalcFeeForTransaction(builder);

                builder.SendFees(newEstimate - totalFeeSent);
                totalFeeSent = newEstimate;

            } while (totalFeeSent + dustAmount > sentAmount);
        }

        public void SendWithChange(TransactionBuilder builder, TransactionBuildContext context, List<ICoin> coins, IDestination destination, Money amount, IDestination changeDestination)
        {
            var orderedCounts = coins.OrderBy(o => o.Amount).ToList();
            var sendAmount = Money.Zero;
            int cnt = 0;
            while (sendAmount < amount && cnt < orderedCounts.Count)
            {
                sendAmount += orderedCounts[cnt].TxOut.Value;
                cnt++;
            }
            if (sendAmount < amount)
                throw new BackendException($"The sum of total applicable outputs is less than the required: {amount.Satoshi} satoshis.", ErrorCode.NotEnoughBitcoinAvailable);

            context.AddCoins(orderedCounts.Take(cnt));
            builder.AddCoins(orderedCounts.Take(cnt));
            builder.Send(destination, amount);

            if (sendAmount - amount > 0)
                builder.Send(changeDestination, sendAmount - amount);
        }

        public void SendAssetWithChange(TransactionBuilder builder, TransactionBuildContext context, List<ColoredCoin> coins, IDestination destination, AssetMoney amount,
            IDestination changeDestination)
        {
            var orderedCounts = coins.OrderBy(o => o.Amount).ToList();
            var sendAmount = new AssetMoney(amount.Id);
            int cnt = 0;
            while (sendAmount < amount && cnt < orderedCounts.Count)
            {
                sendAmount += orderedCounts[cnt].Amount;
                cnt++;
            }
            if (sendAmount < amount)
                throw new BackendException($"The sum of total applicable outputs is less than the required: {amount.Quantity} {amount.Id}.", ErrorCode.NotEnoughAssetAvailable);

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

        public async Task AddFee(Transaction tr)
        {
            var fee = await _feeProvider.CalcFeeForTransaction(tr);
            var providedAmount = Money.Zero;
            var queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();
            do
            {
                var feeInput = await queue.DequeueCoin();
                tr.Inputs.Add(new TxIn
                {
                    PrevOut = feeInput.Outpoint
                });
                providedAmount += feeInput.Amount;

                fee = await _feeProvider.CalcFeeForTransaction(tr);
                if (fee < providedAmount)
                {
                    tr.Outputs.Add(new TxOut(providedAmount - fee, feeInput.ScriptPubKey));
                    return;
                }

            } while (true);
        }
    }
}
