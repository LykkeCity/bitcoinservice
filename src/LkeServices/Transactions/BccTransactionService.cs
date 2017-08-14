using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Common.Log;
using Core;
using Core.Bcc;
using Core.Bitcoin;
using Core.Exceptions;
using Core.OpenAssets;
using Core.Outputs;
using Core.Providers;
using Core.Repositories.TransactionOutputs;
using LkeServices.Providers;
using NBitcoin;
using NBitcoin.Protocol;

namespace LkeServices.Transactions
{
    public interface IBccTransactionService
    {
        Task Transfer(BitcoinAddress from, BitcoinAddress to, decimal amount);
    }


    public class BccTransactionService : IBccTransactionService
    {
        private readonly IBccOutputService _bccOutputService;
        private readonly ISpentOutputRepository _spentOutputRepository;
        private readonly RpcConnectionParams _connectionParams;
        private readonly ITransactionBuildHelper _transactionBuildHelper;
        private readonly ILog _log;
        private readonly IRpcBitcoinClient _rpcBitcoinClient;
        private ISignatureApiProvider _signatureApi;

        public BccTransactionService(IBccOutputService bccOutputService, [KeyFilter(Constants.BccKey)] ISpentOutputRepository spentOutputRepository,
            [KeyFilter(Constants.BccKey)] RpcConnectionParams connectionParams, ITransactionBuildHelper transactionBuildHelper,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            ILog log, [KeyFilter(Constants.BccKey)] IRpcBitcoinClient rpcBitcoinClient)
        {
            _bccOutputService = bccOutputService;
            _spentOutputRepository = spentOutputRepository;
            _connectionParams = connectionParams;
            _transactionBuildHelper = transactionBuildHelper;
            _log = log;
            _rpcBitcoinClient = rpcBitcoinClient;
            _signatureApi = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
        }

        public async Task Transfer(BitcoinAddress @from, BitcoinAddress to, decimal amount)
        {
            var amountMoney = Money.FromUnit(amount, MoneyUnit.BTC);
            var coins = (await _bccOutputService.GetUnspentOutputs(from.ToString())).OfType<Coin>().Cast<ICoin>().ToList();

            var availableAmount = coins.Select(o => o.Amount).DefaultIfEmpty().Select(o => (Money)o ?? Money.Zero).Sum();

            await _log.WriteInfoAsync(nameof(BccTransactionService), nameof(Transfer), null,
                $"Available amount of {from.ToString()} - {availableAmount} satoshis");

            var builder = new TransactionBuilder();
            var context = new TransactionBuildContext(_connectionParams.Network, null, null);
            await _transactionBuildHelper.SendWithChange(builder, context, coins, to, amountMoney, from, false);

            var trId = Guid.NewGuid();

            var transaction = builder.BuildTransaction(true);

            var fee = await _transactionBuildHelper.CalcFee(transaction);

            var output = transaction.Outputs.First(o => o.ScriptPubKey.GetDestinationAddress(_connectionParams.Network) == to);

            output.Value -= fee;

            var signedTr = await _signatureApi.SignBccTransaction(transaction.ToHex());

            var signed = new Transaction(signedTr);
            await _rpcBitcoinClient.BroadcastTransaction(signed, trId);

            await SaveSpentOutputs(trId, transaction);

            await _log.WriteInfoAsync(nameof(BccTransactionService), nameof(Transfer), null,
                $"Broadcast BCC transaction {signed.GetHash()}");
        }


        public async Task SaveSpentOutputs(Guid transactionId, Transaction transaction)
        {
            await _spentOutputRepository.InsertSpentOutputs(transactionId, transaction.Inputs.Select(o => new Output(o.PrevOut)));
        }
    }
}
