using System.Threading.Tasks;
using BitcoinApi.Filters;
using BitcoinApi.Models;
using BitcoinApi.Models.Offchain;
using Core.Exceptions;
using Core.Repositories.Assets;
using LkeServices.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class OffchainController : Controller
    {
        private readonly IOffchainTransactionBuilderService _offchainTransactionBuilder;
        private readonly IAssetRepository _assetRepository;

        public OffchainController(IOffchainTransactionBuilderService offchainTransactionBuilder, IAssetRepository assetRepository)
        {
            _offchainTransactionBuilder = offchainTransactionBuilder;
            _assetRepository = assetRepository;
        }

        [HttpPost("transfer")]
        [ProducesResponseType(typeof(OffchainResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainResponse> Transfer([FromBody]TransferModel model)
        {
            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var tr = await _offchainTransactionBuilder.CreateTransfer(model.ClientPubKey, model.Amount, asset, model.ClientPrevPrivateKey);
            return new OffchainResponse(tr);
        }

        [HttpPost("createchannel")]
        [ProducesResponseType(typeof(OffchainResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainResponse> CreateUnsignedChannel([FromBody]CreateChannelModel model)
        {
            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var tr = await _offchainTransactionBuilder.CreateUnsignedChannel(model.ClientPubKey, model.HotWalletPubKey, model.ClientAmount, model.HubAmount, asset);
            return new OffchainResponse(tr);
        }

        [HttpPost("createhubcommitment")]
        [ProducesResponseType(typeof(OffchainResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainResponse> CreateHubCommitment([FromBody] CreateHubCommitmentModel model)
        {
            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var tr = await _offchainTransactionBuilder.CreateHubCommitment(model.ClientPubKey, asset, model.Amount, model.SignedByClientChannel);
            return new OffchainResponse(tr);
        }

        [HttpPost("finalize")]
        [ProducesResponseType(typeof(OffchainResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainResponse> Finalize([FromBody] FinalizeChannelModel model)
        {
            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var tr = await _offchainTransactionBuilder.Finalize(model.ClientPubKey, model.HotWalletPubKey, asset, model.ClientRevokePubKey, model.SignedByClientHubCommitment);
            return new OffchainResponse(tr);
        }

        [HttpPost("broadcastcommitment")]
        [ProducesResponseType(typeof(TransactionHashResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<TransactionHashResponse> BroadcastCommitment([FromBody]BroadcastCommitmentModel model)
        {
            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);
            return new TransactionHashResponse(await _offchainTransactionBuilder.BroadcastCommitment(model.ClientPubKey, asset, model.Transaction));
        }
    }

}
