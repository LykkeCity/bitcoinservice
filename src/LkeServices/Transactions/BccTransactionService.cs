using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Common;
using Common.Log;
using Core;
using Core.Bcc;
using Core.Bitcoin;
using Core.Exceptions;
using Core.OpenAssets;
using Core.Outputs;
using Core.Providers;
using Core.Repositories.Assets;
using Core.Repositories.Offchain;
using Core.Repositories.TransactionOutputs;
using LkeServices.Multisig;
using LkeServices.Providers;
using NBitcoin;
using NBitcoin.Policy;
using NBitcoin.Protocol;

namespace LkeServices.Transactions
{
    public interface IBccTransactionService
    {
        Task Transfer(BitcoinAddress from, BitcoinAddress to, decimal amount);
        Task<BccSplitResult> CreateSplitTransaction(string multisig, BitcoinAddress clientDest, BitcoinAddress hubDest);
        Task<BccTransaction> CreatePrivateTransfer(BitcoinAddress @from, BitcoinAddress to, decimal fee);
        Task<string> Broadcast(string transaction, Guid? trId);
    }


    public class BccTransactionService : IBccTransactionService
    {
        private const long Dust = 2700;

        private readonly IBccOutputService _bccOutputService;
        private readonly ISpentOutputRepository _spentOutputRepository;
        private readonly RpcConnectionParams _connectionParams;
        private readonly ITransactionBuildHelper _transactionBuildHelper;
        private readonly ILog _log;
        private readonly IRpcBitcoinClient _rpcBitcoinClient;
        private readonly IMultisigService _multisigService;
        private readonly IOffchainChannelRepository _offchainChannelRepository;
        private readonly ICommitmentRepository _commitmentRepository;
        private readonly ISignatureApiProvider _signatureApi;

        public BccTransactionService(IBccOutputService bccOutputService, [KeyFilter(Constants.BccKey)] ISpentOutputRepository spentOutputRepository,
            [KeyFilter(Constants.BccKey)] RpcConnectionParams connectionParams, ITransactionBuildHelper transactionBuildHelper,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            ILog log, [KeyFilter(Constants.BccKey)] IRpcBitcoinClient rpcBitcoinClient, IMultisigService multisigService,
            IOffchainChannelRepository offchainChannelRepository,
            ICommitmentRepository commitmentRepository
            )
        {
            _bccOutputService = bccOutputService;
            _spentOutputRepository = spentOutputRepository;
            _connectionParams = connectionParams;
            _transactionBuildHelper = transactionBuildHelper;
            _log = log;
            _rpcBitcoinClient = rpcBitcoinClient;
            _multisigService = multisigService;
            _offchainChannelRepository = offchainChannelRepository;
            _commitmentRepository = commitmentRepository;
            _signatureApi = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
        }

        public async Task Transfer(BitcoinAddress from, BitcoinAddress to, decimal amount)
        {
            var amountMoney = Money.FromUnit(amount, MoneyUnit.BTC);
            var coins = (await _bccOutputService.GetUnspentOutputs(from.ToString())).OfType<Coin>().Cast<ICoin>().ToList();

            var availableAmount = coins.Select(o => o.Amount).DefaultIfEmpty().Select(o => (Money)o ?? Money.Zero).Sum();

            await _log.WriteInfoAsync(nameof(BccTransactionService), nameof(Transfer), null,
                $"Available amount of {from} - {availableAmount} satoshis");

            var builder = new TransactionBuilder();
            var context = new TransactionBuildContext(_connectionParams.Network, null, null);
            await _transactionBuildHelper.SendWithChange(builder, context, coins, to, amountMoney, from, false);

            var trId = Guid.NewGuid();

            var transaction = builder.BuildTransaction(true);

            var fee = await _transactionBuildHelper.CalcFee(transaction);

            var output = transaction.Outputs.First(o => o.ScriptPubKey.GetDestinationAddress(_connectionParams.Network) == to);

            output.Value -= fee;

            var signedTr = await _signatureApi.SignBccTransaction(transaction.ToHex());

            await Broadcast(signedTr, trId);
        }

        public async Task<string> Broadcast(string transaction, Guid? trId)
        {
            trId = trId ?? Guid.NewGuid();
            var tr = new Transaction(transaction);
            await _rpcBitcoinClient.BroadcastTransaction(tr, trId.Value);

            await SaveSpentOutputs(trId.Value, tr);
            await _log.WriteInfoAsync(nameof(BccTransactionService), nameof(Broadcast), null,
                $"Broadcast BCC transaction {tr.GetHash()}");
            return tr.GetHash().ToString();
        }

        public async Task<BccSplitResult> CreateSplitTransaction(string multisig, BitcoinAddress clientDest, BitcoinAddress hubDest)
        {
            var wallet = await _multisigService.GetMultisigByAddr(multisig);
            if (wallet == null)
                throw new BackendException($"Multisig {multisig} is not registered", ErrorCode.BadInputParameter);
            var channels = await _offchainChannelRepository.GetChannels(multisig, "BTC");

            var outputs = (await _bccOutputService.GetUnspentOutputs(multisig)).OfType<Coin>().ToList();
            if (!outputs.Any())
                throw new BackendException("Address has no unspent outputs", ErrorCode.NoCoinsFound);

            var clientAmount = Money.Zero;
            var hubAmount = Money.Zero;

            var channel = channels.Where(o => o.CreateDt <= Constants.PrevBccBlockTime && o.IsBroadcasted &&
                                             (o.Actual || o.BsonCreateDt > Constants.PrevBccBlockTime))
                                  .OrderByDescending(o => o.CreateDt).FirstOrDefault();
            var totalBalance = outputs.Select(o => o.Amount).Sum();
            if (channel == null)
                clientAmount = totalBalance;
            else
            {
                var commitments = await _commitmentRepository.GetCommitments(channel.ChannelId);
                var commitment = commitments.Where(o => o.Type == CommitmentType.Client && o.CreateDt < Constants.PrevBccBlockTime)
                    .OrderByDescending(o => o.CreateDt).FirstOrDefault();

                if (totalBalance.ToDecimal(MoneyUnit.BTC) < commitment.ClientAmount + commitment.HubAmount)
                    throw new BackendException($"Multisig {multisig} balance is less than channel amounts", ErrorCode.NotEnoughBitcoinAvailable);
                hubAmount = Money.FromUnit(commitment.HubAmount, MoneyUnit.BTC);
                clientAmount = totalBalance - hubAmount;
            }

            var builder = new TransactionBuilder();
            builder.DustPrevention = false;
            builder.AddCoins(outputs);
            if (clientAmount > 0)
                builder.Send(clientDest, clientAmount);
            if (hubAmount > 0)
                builder.Send(hubDest, hubAmount);

            var tr = builder.BuildTransaction(true);

            var fee = await _transactionBuildHelper.CalcFee(tr);

            if (totalBalance < fee)
                throw new BackendException($"Multisig {multisig} balance is less than fee", ErrorCode.NotEnoughBitcoinAvailable);

            var initialClientAmount = clientAmount;
            clientAmount = SendFees(clientDest, clientAmount, fee, totalBalance, tr);
            hubAmount = SendFees(hubDest, hubAmount, fee, totalBalance, tr);

            var signed = await _signatureApi.SignBccTransaction(tr.ToHex());

            var bccOutputs = tr.Inputs.Select(o =>
            {
                var txOut = outputs.First(c => c.Outpoint == o.PrevOut).TxOut;
                return new BccOutput
                {
                    Satoshis = txOut.Value.Satoshi,
                    Script = txOut.ScriptPubKey.ToHex()
                };
            }).ToList();

            return new BccSplitResult
            {
                Transaction = new BccTransaction
                {
                    TransactionHex = signed,
                    Outputs = bccOutputs.ToJson().ToLower()
                },
                ClientAmount = clientAmount.ToDecimal(MoneyUnit.BTC),
                HubAmount = hubAmount.ToDecimal(MoneyUnit.BTC),
                ClientFeeAmount = (initialClientAmount - clientAmount).ToDecimal(MoneyUnit.BTC)
            };
        }

        private static Money SendFees(BitcoinAddress destination, Money amount, Money fee, Money totalBalance, Transaction tr)
        {
            if (amount > 0)
            {
                var feePart = Money.FromUnit((long)(fee.Satoshi * ((double)amount.Satoshi / totalBalance.Satoshi)), MoneyUnit.Satoshi);
                var output = tr.Outputs.First(o => o.ScriptPubKey == destination.ScriptPubKey);
                output.Value -= feePart;
                if (output.Value.Satoshi > Dust)
                    return output.Value;
                tr.Outputs.Remove(output);
            }
            return Money.Zero;
        }

        public async Task<BccTransaction> CreatePrivateTransfer(BitcoinAddress from, BitcoinAddress to, decimal fee)
        {
            var outputs = (await _bccOutputService.GetUnspentOutputs(from.ToString())).OfType<Coin>().ToList();
            if (!outputs.Any())
                throw new BackendException("Address has no unspent outputs", ErrorCode.NoCoinsFound);
            var totalBalance = outputs.Select(o => o.Amount).Sum();
            var feeAmount = Money.FromUnit(fee, MoneyUnit.BTC);

            if (totalBalance < feeAmount)
                throw new BackendException("Total balance is less than fee", ErrorCode.NotEnoughBitcoinAvailable);

            var builder = new TransactionBuilder();
            builder.AddCoins(outputs);
            builder.SendFees(feeAmount);
            builder.Send(to, totalBalance - feeAmount);
            var tr = builder.BuildTransaction(true);

            var bccOutputs = tr.Inputs.Select(o =>
            {
                var txOut = outputs.First(c => c.Outpoint == o.PrevOut).TxOut;
                return new BccOutput
                {
                    Satoshis = txOut.Value.Satoshi,
                    Script = txOut.ScriptPubKey.ToHex()
                };
            }).ToList();

            return new BccTransaction
            {
                TransactionHex = tr.ToHex(),
                Outputs = bccOutputs.ToJson().ToLower()
            };
        }


        public async Task SaveSpentOutputs(Guid transactionId, Transaction transaction)
        {
            await _spentOutputRepository.InsertSpentOutputs(transactionId, transaction.Inputs.Select(o => new Output(o.PrevOut)));
        }
    }


}
