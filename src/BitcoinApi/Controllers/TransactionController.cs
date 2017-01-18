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
using Core.Helpers;
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
        private readonly ILog _log;
        private readonly ITransactionSignRequestRepository _transactionSignRequestRepository;
        private readonly IBitcoinBroadcastService _broadcastService;

        public TransactionController(ILykkeTransactionBuilderService builder,
            IAssetRepository assetRepository,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            ILog log,
            ITransactionSignRequestRepository transactionSignRequestRepository,
            IBitcoinBroadcastService broadcastService)
        {
            _builder = builder;
            _assetRepository = assetRepository;
            _signatureApiProvider = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
            _log = log;
            _transactionSignRequestRepository = transactionSignRequestRepository;
            _broadcastService = broadcastService;
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
            await Log("Transfer", "Begin", model);

            var sourceAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.SourceAddress);
            if (sourceAddress == null)
                throw new BackendException("Invalid source address provided", ErrorCode.InvalidAddress);

            var destAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.DestinationAddress);
            if (destAddress == null)
                throw new BackendException("Invalid destination address provided", ErrorCode.InvalidAddress);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId);

            var createTransactionResponse = await _builder.GetTransferTransaction(sourceAddress, destAddress, model.Amount, asset, transactionId);

            await Log("Transfer", "End", model, createTransactionResponse.TransactionId);

            return Ok(new TransactionResponse
            {
                Transaction = createTransactionResponse.Transaction,
                TransactionId = createTransactionResponse.TransactionId
            });
        }

        /// <summary>
        /// Creates transfer all transaction without signs
        /// </summary>
        /// <returns>Transaction (hex) and internal transaction id</returns>
        [HttpPost("transferall")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateTransferAll([FromBody]TransferAllRequest model)
        {
            await Log("TransferAll", "Begin", model);

            var sourceAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.SourceAddress);
            if (sourceAddress == null)
                throw new BackendException("Invalid source address provided", ErrorCode.InvalidAddress);

            var destAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.DestinationAddress);
            if (destAddress == null)
                throw new BackendException("Invalid destination address provided", ErrorCode.InvalidAddress);

            var transactionId = await _builder.AddTransactionId(model.TransactionId);

            var createTransactionResponse = await _builder.GetTransferAllTransaction(sourceAddress, destAddress, transactionId);

            await Log("TransferAll", "End", model, createTransactionResponse.TransactionId);
            return Ok(new TransactionResponse
            {
                Transaction = createTransactionResponse.Transaction,
                TransactionId = createTransactionResponse.TransactionId
            });
        }



        /// <summary>
        /// Creates swap transaction without signs
        /// </summary>
        /// <returns>Transaction (hex) and internal transaction id</returns>
        [HttpPost("swap")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateSwap([FromBody]SwapRequest model)
        {
            await Log("Swap", "Begin", model);

            var bitcoinAddres1 = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.MultisigCustomer1);
            if (bitcoinAddres1 == null)
                throw new BackendException("Invalid MultisigCustomer1 provided", ErrorCode.InvalidAddress);

            var bitcoinAddres2 = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.MultisigCustomer2);
            if (bitcoinAddres2 == null)
                throw new BackendException("Invalid MultisigCustomer2 provided", ErrorCode.InvalidAddress);

            var asset1 = await _assetRepository.GetAssetById(model.Asset1);
            if (asset1 == null)
                throw new BackendException("Provided Asset1 is missing in database", ErrorCode.AssetNotFound);

            var asset2 = await _assetRepository.GetAssetById(model.Asset2);
            if (asset2 == null)
                throw new BackendException("Provided Asset2 is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId);

            var createTransactionResponse = await _builder.GetSwapTransaction(bitcoinAddres1, model.Amount1, asset1, bitcoinAddres2, model.Amount2, asset2, transactionId);

            await Log("Swap", "End", model, createTransactionResponse.TransactionId);

            return Ok(new TransactionResponse
            {
                Transaction = createTransactionResponse.Transaction,
                TransactionId = createTransactionResponse.TransactionId
            });
        }

        /// <summary>
        /// Creates issue transaction without sign
        /// </summary>
        /// <returns>Transaction (hex) and internal transaction id</returns>
        [HttpPost("issue")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> Issue([FromBody] IssueRequest model)
        {
            await Log("Issue", "Begin", model);

            var bitcoinAddres = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.Address);
            if (bitcoinAddres == null)
                throw new BackendException("Invalid Address provided", ErrorCode.InvalidAddress);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided Asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId);

            var createTransactionResponse = await _builder.GetIssueTransaction(bitcoinAddres, model.Amount, asset, transactionId);

            var signedTrHex = await _signatureApiProvider.SignTransaction(createTransactionResponse.Transaction);

            var signedTr = new Transaction(signedTrHex);

            await _broadcastService.BroadcastTransaction(createTransactionResponse.TransactionId, signedTr);

            await Log("Issue", "End", model, createTransactionResponse.TransactionId);

            return Ok(new TransactionResponse
            {
                TransactionId = createTransactionResponse.TransactionId
            });
        }

        /// <summary>
        /// Creates uncolor transaction without sign
        /// </summary>
        /// <returns>Transaction (hex) and internal transaction id</returns>
        [HttpPost("destroy")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> Destroy([FromBody] DestroyRequest model)
        {
            await Log("Destroy", "Begin", model);

            var bitcoinAddres = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.Address);
            if (bitcoinAddres == null)
                throw new BackendException("Invalid Address provided", ErrorCode.InvalidAddress);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided Asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId);

            var createTransactionResponse = await _builder.GetDestroyTransaction(bitcoinAddres, model.Amount, asset, transactionId);

            await Log("Destroy", "End", model, createTransactionResponse.TransactionId);

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
            await Log("Broadcast", "Begin", model);

            var signRequest = await _transactionSignRequestRepository.GetSignRequest(model.TransactionId);

            if (!TransactionComparer.CompareTransactions(signRequest.InitialTransaction, model.Transaction))
                throw new BackendException("Signed transaction is not equals to initial transaction", ErrorCode.BadTransaction);

            var result = await _transactionSignRequestRepository.SetSignedTransaction(model.TransactionId, model.Transaction);
            if (result.SignedTransaction1 != null && result.RequiredSignCount == 1 ||
                result.SignedTransaction1 != null && result.SignedTransaction2 != null && result.RequiredSignCount == 2)
            {
                var tr = result.SignedTransaction1;
                if (result.RequiredSignCount == 2)
                    tr = OpenAssetsHelper.MergeTransactionsSignatures(result.SignedTransaction1, result.SignedTransaction2);

                var fullSignedHex = await _signatureApiProvider.SignTransaction(tr);
                var fullSigned = new Transaction(fullSignedHex);

                await _broadcastService.BroadcastTransaction(model.TransactionId, fullSigned);
            }
            await Log("Broadcast", "End", model);
        }

        private async Task Log(string method, string status, object model, Guid? transactionId = null)
        {
            var properties = model.GetType().GetTypeInfo().GetProperties();
            var builder = new StringBuilder();
            foreach (var prop in properties)
                builder.Append($"{prop.Name}: [{prop.GetValue(model)}], ");

            if (transactionId.HasValue)
                builder.Append($"Transaction: [{transactionId}]");

            await _log.WriteInfoAsync("TransactionController", method, status, builder.ToString());
        }
    }
}
