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
        [ProducesResponseType(typeof(OffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainApiResponse> Transfer([FromBody]TransferModel model)
        {
            var asset = await GetAsset(model.Asset);
            var tr = await _offchainTransactionBuilder.CreateTransfer(model.ClientPubKey, model.Amount, asset, model.ClientPrevPrivateKey, model.RequiredOperation, model.TransferId);
            return new OffchainApiResponse(tr);
        }

        [HttpPost("createchannel")]
        [ProducesResponseType(typeof(OffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainApiResponse> CreateUnsignedChannel([FromBody]CreateChannelModel model)
        {
            var asset = await GetAsset(model.Asset);
            var tr = await _offchainTransactionBuilder.CreateUnsignedChannel(model.ClientPubKey, model.HotWalletPubKey, model.HubAmount, asset
                , model.RequiredOperation, model.TransferId);
            return new OffchainApiResponse(tr);
        }


        [HttpPost("createcashin")]
        [ProducesResponseType(typeof(OffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainApiResponse> CreateCashin([FromBody]CreateCashinModel model)
        {
            var asset = await GetAsset(model.Asset);
            var tr = await _offchainTransactionBuilder.CreateCashin(model.ClientPubKey, model.Amount, asset, model.CashinAddress, model.TransferId);
            return new OffchainApiResponse(tr);
        }


        [HttpPost("createhubcommitment")]
        [ProducesResponseType(typeof(OffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainApiResponse> CreateHubCommitment([FromBody] CreateHubCommitmentModel model)
        {
            var asset = await GetAsset(model.Asset);
            var tr = await _offchainTransactionBuilder.CreateHubCommitment(model.ClientPubKey, asset, model.Amount, model.SignedByClientChannel);
            return new OffchainApiResponse(tr);
        }

        [HttpPost("finalize")]
        [ProducesResponseType(typeof(OffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainApiResponse> Finalize([FromBody] FinalizeChannelModel model)
        {
            var asset = await GetAsset(model.Asset);
            var tr = await _offchainTransactionBuilder.Finalize(model.ClientPubKey, model.HotWalletPubKey, asset, model.ClientRevokePubKey, model.SignedByClientHubCommitment, model.TransferId);
            return new OffchainApiResponse(tr);
        }

        [HttpPost("broadcastcommitment")]
        [ProducesResponseType(typeof(TransactionHashResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<TransactionHashResponse> BroadcastCommitment([FromBody]BroadcastCommitmentModel model)
        {
            var asset = await GetAsset(model.Asset);
            return new TransactionHashResponse(await _offchainTransactionBuilder.BroadcastCommitment(model.ClientPubKey, asset, model.Transaction));
        }

        [HttpPost("closechannel")]
        [ProducesResponseType(typeof(OffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainApiResponse> CloseChannel([FromBody]CloseChannelModel model)
        {
            var asset = await GetAsset(model.Asset);
            return new OffchainApiResponse(await _offchainTransactionBuilder.CloseChannel(model.ClientPubKey, model.CashoutAddress,
                model.HotWalletPubKey, asset));
        }


        [HttpPost("broadcastclosing")]
        [ProducesResponseType(typeof(TransactionHashResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<TransactionHashResponse> BroadcastClosing([FromBody]BroadcastClosingChannelModel model)
        {
            var asset = await GetAsset(model.Asset);
            return new TransactionHashResponse(await _offchainTransactionBuilder.BroadcastClosingChannel(model.ClientPubKey, asset, model.SignedByClientTransaction));
        }

        private async Task<IAsset> GetAsset(string assetId)
        {
            var asset = await _assetRepository.GetAssetById(assetId);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);
            return asset;
        }
    }

}
