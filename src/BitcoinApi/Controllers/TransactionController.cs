﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitcoinApi.Filters;
using BitcoinApi.Models;
using Common.Log;
using Core.Bitcoin;
using Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Core.OpenAssets;
using Core.Providers;
using Core.Repositories.Assets;
using LkeServices.Transactions;
using NBitcoin;
using System.Reflection;
using BitcoinApi.Models.Offchain;
using Common;
using Core.Helpers;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Core.TransactionQueueWriter;
using Core.TransactionQueueWriter.Commands;
using LkeServices.Providers;
using TransactionResponse = BitcoinApi.Models.TransactionResponse;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class TransactionController : Controller
    {
        private readonly ILykkeTransactionBuilderService _builder;
        private readonly IAssetRepository _assetRepository;
        private readonly ISignatureApiProvider _signatureApiProvider;
        private readonly ITransactionSignRequestRepository _transactionSignRequestRepository;
        private readonly ITransactionBlobStorage _transactionBlobStorage;
        private readonly IBitcoinBroadcastService _broadcastService;
        private readonly IBroadcastedTransactionRepository _broadcastedTransactionRepository;
        private readonly IOffchainService _offchainService;


        public TransactionController(ILykkeTransactionBuilderService builder,
            IAssetRepository assetRepository,
            ISignatureApiProvider signatureApiProvider,
            ITransactionSignRequestRepository transactionSignRequestRepository,
            ITransactionBlobStorage transactionBlobStorage,
            IBitcoinBroadcastService broadcastService, IBroadcastedTransactionRepository broadcastedTransactionRepository, IOffchainService offchainService)
        {
            _builder = builder;
            _assetRepository = assetRepository;
            _signatureApiProvider = signatureApiProvider;
            _transactionSignRequestRepository = transactionSignRequestRepository;
            _transactionBlobStorage = transactionBlobStorage;
            _broadcastService = broadcastService;
            _broadcastedTransactionRepository = broadcastedTransactionRepository;
            _offchainService = offchainService;
        }

        /// <summary>
        /// Creates cash out transaction without signs
        /// </summary>
        /// <returns>Transaction (hex) and internal transaction id</returns>
        [HttpPost("transfer")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateCashout([FromBody]TransferRequest model)
        {
            if (model.Amount <= 0)
                throw new BackendException("Amount can't be less or equal to zero", ErrorCode.BadInputParameter);

            var sourceAddress = OpenAssetsHelper.ParseAddress(model.SourceAddress);
            if (sourceAddress == null)
                throw new BackendException("Invalid source address provided", ErrorCode.InvalidAddress);

            var destAddress = OpenAssetsHelper.ParseAddress(model.DestinationAddress);
            if (destAddress == null)
                throw new BackendException("Invalid destination address provided", ErrorCode.InvalidAddress);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            if (model.Fee.GetValueOrDefault() < 0)
                throw new BackendException("Fee must be greater than or equal to zero", ErrorCode.BadInputParameter);

            if (model.Amount <= model.Fee.GetValueOrDefault())
                throw new BackendException("Amount is less than fee", ErrorCode.BadInputParameter);

            var transactionId = await _builder.AddTransactionId(model.TransactionId, model.ToJson());

            CreateTransactionResponse createTransactionResponse;

            if (OpenAssetsHelper.IsBitcoin(asset.Id) && model.Fee.HasValue)
            {
                createTransactionResponse = await _builder.GetPrivateTransferTransaction(sourceAddress, destAddress, model.Amount,
                    model.Fee.Value, transactionId);
                await _transactionSignRequestRepository.DoNotSign(transactionId);
            }
            else
                createTransactionResponse = await _builder.GetTransferTransaction(sourceAddress, destAddress, model.Amount, asset, transactionId, true, true);

            await _transactionBlobStorage.AddOrReplaceTransaction(transactionId, TransactionBlobType.Initial, createTransactionResponse.Transaction);

            return Ok(new TransactionResponse
            {
                Transaction = createTransactionResponse.Transaction,
                TransactionId = createTransactionResponse.TransactionId,
                Fee = (createTransactionResponse as PrivateTransferResponse)?.Fee ?? 0
            });
        }


        /// <summary>
        ///  Broadcast fully signed bitcoin transaction to network
        /// </summary>
        [HttpPost("broadcast")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task Broadcast([FromBody] BroadcastTransactionRequest model)
        {
            var signRequest = await _transactionSignRequestRepository.GetSignRequest(model.TransactionId);

            if (signRequest == null)
                throw new BackendException("Transaction is not found", ErrorCode.BadTransaction);

            if (signRequest.Invalidated == true)
                throw new BackendException("Transaction was invalidated", ErrorCode.BadTransaction);

            var initialTransaction = await _transactionBlobStorage.GetTransaction(model.TransactionId, TransactionBlobType.Initial);

            if (!TransactionComparer.CompareTransactions(initialTransaction, model.Transaction))
                throw new BackendException("Signed transaction is not equals to initial transaction", ErrorCode.BadTransaction);

            var transaction = new Transaction(model.Transaction);

            if (transaction.Inputs.All(o => o.ScriptSig == null || o.ScriptSig.Length == 0))
                throw new BackendException("Transaction is not signed by client", ErrorCode.BadTransaction);

            var fullSignedHex = signRequest.DoNotSign ? model.Transaction : await _signatureApiProvider.SignTransaction(model.Transaction);

            await _transactionBlobStorage.AddOrReplaceTransaction(model.TransactionId, TransactionBlobType.Signed, fullSignedHex);

            var fullSigned = new Transaction(fullSignedHex);

            await _broadcastService.BroadcastTransaction(model.TransactionId, fullSigned, useHandlers: false, savePaidFees: !signRequest.DoNotSign);
        }

        /// <summary>
        /// Broadcast multiple transfer transaction
        /// </summary>
        /// <returns>Internal transaction id and hash</returns>
        [HttpPost("multipletransfer")]
        [ProducesResponseType(typeof(TransactionIdAndHashResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateMultipleTransfer([FromBody]MultipleTransferRequest model)
        {
            foreach (var source in model.Sources)
                await ValidateAddress(source.Address);

            if (model.FixedFee.GetValueOrDefault() < 0)
                throw new BackendException("Fixed fee must be greater than or equal to zero", ErrorCode.BadInputParameter);

            var destAddress = OpenAssetsHelper.ParseAddress(model.Destination);
            if (destAddress == null)
                throw new BackendException("Invalid destination address provided", ErrorCode.InvalidAddress);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId, $"MultipleTransfer: {model.ToJson()}");

            var response = await _builder.GetMultipleTransferTransaction(destAddress, asset,
                model.Sources.ToDictionary(x => x.Address, x => x.Amount), model.FeeRate, model.FixedFee.GetValueOrDefault(), transactionId);

            var fullSignedHex = await _signatureApiProvider.SignTransaction(response.Transaction);

            await _transactionBlobStorage.AddOrReplaceTransaction(transactionId, TransactionBlobType.Signed, fullSignedHex);

            var fullSigned = new Transaction(fullSignedHex);

            await _broadcastService.BroadcastTransaction(transactionId, fullSigned, useHandlers: false);

            return Ok(new TransactionIdAndHashResponse
            {
                TransactionId = transactionId,
                Hash = fullSigned.GetHash().ToString()
            });
        }

        private async Task ValidateAddress(string address, bool checkOffchain = true)
        {
            var bitcoinAddres = OpenAssetsHelper.ParseAddress(address);
            if (bitcoinAddres == null)
                throw new BackendException($"Invalid Address provided: {address}", ErrorCode.InvalidAddress);
            if (checkOffchain && await _offchainService.HasChannel(address))
                throw new BackendException("Address was used in offchain", ErrorCode.AddressUsedInOffchain);
        }

        /// <summary>
        ///  Return transaction hash by internal id
        /// </summary>
        [HttpGet("{transactionId}")]
        [ProducesResponseType(typeof(TransactionHashResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> Get(Guid transactionId)
        {
            var tr = await _broadcastedTransactionRepository.GetTransactionById(transactionId);

            if (tr == null)
                throw new BackendException("Transaction was not found", ErrorCode.BadInputParameter);

            return Ok(new TransactionHashResponse(tr.Hash));
        }
    }
}
