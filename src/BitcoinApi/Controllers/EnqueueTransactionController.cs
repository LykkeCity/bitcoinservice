using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BitcoinApi.Filters;
using BitcoinApi.Models;
using BitcoinApi.Services;
using Common;
using Common.Log;
using Core.Exceptions;
using Core.OpenAssets;
using Core.Repositories.Assets;
using Core.TransactionQueueWriter;
using Core.TransactionQueueWriter.Commands;
using LkeServices.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class EnqueueTransactionController : Controller
    {
        private readonly ILykkeTransactionBuilderService _builder;
        private readonly IAssetRepository _assetRepository;
        private readonly OffchainService _offchainService;
        private readonly ITransactionQueueWriter _transactionQueueWriter;
        private readonly IRetryFailedTransactionService _retryFailedService;

        public EnqueueTransactionController(ILykkeTransactionBuilderService builder,
            IAssetRepository assetRepository,
            OffchainService offchainService,
            ITransactionQueueWriter transactionQueueWriter, IRetryFailedTransactionService retryFailedService)
        {
            _builder = builder;
            _assetRepository = assetRepository;
            _offchainService = offchainService;
            _transactionQueueWriter = transactionQueueWriter;
            _retryFailedService = retryFailedService;
        }



        /// <summary>
        /// Add transfer transaction to queue for building
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("transfer")]
        [ProducesResponseType(typeof(TransactionIdResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateCashout([FromBody]TransferRequest model)
        {
            if (model.Amount <= 0)
                throw new BackendException("Amount can't be less or equal to zero", ErrorCode.BadInputParameter);

            await ValidateAddress(model.SourceAddress);
            await ValidateAddress(model.DestinationAddress);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId, $"Transfer: {model.ToJson()}");

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.Transfer, new TransferCommand
            {
                Amount = model.Amount,
                SourceAddress = model.SourceAddress,
                Asset = model.Asset,
                DestinationAddress = model.DestinationAddress
            }.ToJson());

            return Ok(new TransactionIdResponse
            {
                TransactionId = transactionId
            });
        }

        /// <summary>
        /// Add transfer all transaction to queue for building
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("transferall")]
        [ProducesResponseType(typeof(TransactionIdResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateTransferAll([FromBody]TransferAllRequest model)
        {
            await ValidateAddress(model.SourceAddress, false);
            await ValidateAddress(model.DestinationAddress, false);

            var transactionId = await _builder.AddTransactionId(model.TransactionId, $"TransferAll: {model.ToJson()}");

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.TransferAll, new TransferAllCommand
            {
                SourceAddress = model.SourceAddress,
                DestinationAddress = model.DestinationAddress,
            }.ToJson());

            return Ok(new TransactionIdResponse
            {
                TransactionId = transactionId
            });
        }



        /// <summary>
        /// Add swap transaction to queue for building
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("swap")]
        [ProducesResponseType(typeof(TransactionIdResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateSwap([FromBody]SwapRequest model)
        {
            if (model.Amount1 <= 0 || model.Amount2 <= 0)
                throw new BackendException("Amount can't be less or equal to zero", ErrorCode.BadInputParameter);

            await ValidateAddress(model.MultisigCustomer1);
            await ValidateAddress(model.MultisigCustomer2);

            var asset1 = await _assetRepository.GetAssetById(model.Asset1);
            if (asset1 == null)
                throw new BackendException("Provided Asset1 is missing in database", ErrorCode.AssetNotFound);

            var asset2 = await _assetRepository.GetAssetById(model.Asset2);
            if (asset2 == null)
                throw new BackendException("Provided Asset2 is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId, $"Swap: {model.ToJson()}");

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.Swap, new SwapCommand
            {
                MultisigCustomer1 = model.MultisigCustomer1,
                Amount1 = model.Amount1,
                Asset1 = model.Asset1,
                MultisigCustomer2 = model.MultisigCustomer2,
                Amount2 = model.Amount2,
                Asset2 = model.Asset2
            }.ToJson());

            return Ok(new TransactionIdResponse
            {
                TransactionId = transactionId
            });
        }

        /// <summary>
        /// Add issue transaction to queue for building
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("issue")]
        [ProducesResponseType(typeof(TransactionIdResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> Issue([FromBody] IssueRequest model)
        {
            if (model.Amount <= 0)
                throw new BackendException("Amount can't be less or equal to zero", ErrorCode.BadInputParameter);

            await ValidateAddress(model.Address);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided Asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId, $"Issue: {model.ToJson()}");

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.Issue, new IssueCommand
            {
                Amount = model.Amount,
                Asset = model.Asset,
                Address = model.Address
            }.ToJson());

            return Ok(new TransactionIdResponse
            {
                TransactionId = transactionId
            });
        }

        /// <summary>
        /// Add destroy transaction to queue for building
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("destroy")]
        [ProducesResponseType(typeof(TransactionIdResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> Destroy([FromBody] DestroyRequest model)
        {
            if (model.Amount <= 0)
                throw new BackendException("Amount can't be less or equal to zero", ErrorCode.BadInputParameter);

            await ValidateAddress(model.Address);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided Asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId, $"Destroy: {model.ToJson()}");

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.Destroy, new DestroyCommand
            {
                Amount = model.Amount,
                Asset = model.Asset,
                Address = model.Address,
            }.ToJson());

            return Ok(new TransactionIdResponse
            {
                TransactionId = transactionId
            });
        }

        /// <summary>
        /// Add multiple transfer to queue
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("multipletransfer")]
        [ProducesResponseType(typeof(TransactionIdResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateMultipleTransfer([FromBody]MultipleTransferRequest model)
        {
            foreach (var source in model.Sources)
                await ValidateAddress(source.Address);

            var transactionId = await _builder.AddTransactionId(model.TransactionId, $"MultipleTransfer: {model.ToJson()}");

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.MultipleTransfers, new MultipleTransferCommand
            {
                Destination = model.Destination,
                Asset = model.Asset,
                Addresses = model.Sources.ToDictionary(x => x.Address, x => x.Amount)
            }.ToJson());

            return Ok(new TransactionIdResponse
            {
                TransactionId = transactionId
            });
        }

        [HttpPost("retry")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> Retry([FromBody] RetryFailedRequest model)
        {
            await _retryFailedService.RetryTransaction(model.TransactionId);

            return Ok();
        }

        private async Task ValidateAddress(string address, bool checkOffchain = true)
        {
            var bitcoinAddres = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(address);
            if (bitcoinAddres == null)
                throw new BackendException($"Invalid Address provided: {address}", ErrorCode.InvalidAddress);
            if (checkOffchain && await _offchainService.HasChannel(address))
                throw new BackendException("Address was used in offchain", ErrorCode.AddressUsedInOffchain);
        }
    }
}
