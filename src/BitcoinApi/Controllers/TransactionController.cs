using System;
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

        public TransactionController(ILykkeTransactionBuilderService builder,
            IAssetRepository assetRepository,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            ITransactionSignRequestRepository transactionSignRequestRepository,
            ITransactionBlobStorage transactionBlobStorage,
            IBitcoinBroadcastService broadcastService, IBroadcastedTransactionRepository broadcastedTransactionRepository)
        {
            _builder = builder;
            _assetRepository = assetRepository;
            _signatureApiProvider = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
            _transactionSignRequestRepository = transactionSignRequestRepository;
            _transactionBlobStorage = transactionBlobStorage;
            _broadcastService = broadcastService;
            _broadcastedTransactionRepository = broadcastedTransactionRepository;
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

            var sourceAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.SourceAddress);
            if (sourceAddress == null)
                throw new BackendException("Invalid source address provided", ErrorCode.InvalidAddress);

            var destAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.DestinationAddress);
            if (destAddress == null)
                throw new BackendException("Invalid destination address provided", ErrorCode.InvalidAddress);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId, model.ToJson());

            var createTransactionResponse = await _builder.GetTransferTransaction(sourceAddress, destAddress, model.Amount, asset, transactionId, true);

            await _transactionBlobStorage.AddOrReplaceTransaction(transactionId, TransactionBlobType.Initial, createTransactionResponse.Transaction);

            return Ok(new TransactionResponse
            {
                Transaction = createTransactionResponse.Transaction,
                TransactionId = createTransactionResponse.TransactionId
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

            var fullSignedHex = await _signatureApiProvider.SignTransaction(model.Transaction);

            await _transactionBlobStorage.AddOrReplaceTransaction(model.TransactionId, TransactionBlobType.Signed, fullSignedHex);

            var fullSigned = new Transaction(fullSignedHex);

            await _broadcastService.BroadcastTransaction(model.TransactionId, fullSigned, useHandlers: false);
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
