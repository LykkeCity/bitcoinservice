using System.Threading.Tasks;
using BitcoinApi.Filters;
using BitcoinApi.Models;
using BitcoinApi.Models.Offchain;
using Common;
using Core.Exceptions;
using Core.Repositories.Assets;
using LkeServices.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class OffchainController : Controller
    {
        private readonly IOffchainService _offchain;
        private readonly CachedDataDictionary<string, IAsset> _assetRepository;

        public OffchainController(IOffchainService offchain, CachedDataDictionary<string, IAsset> assetRepository)
        {
            _offchain = offchain;
            _assetRepository = assetRepository;
        }

        [HttpPost("transfer")]
        [ProducesResponseType(typeof(OffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainApiResponse> Transfer([FromBody]TransferModel model)
        {
            var asset = await GetAsset(model.Asset);
            var tr = await _offchain.CreateTransfer(model.ClientPubKey, model.Amount, asset, model.ClientPrevPrivateKey, model.RequiredOperation, model.TransferId);
            return new OffchainApiResponse(tr);
        }

        [HttpPost("createchannel")]
        [ProducesResponseType(typeof(OffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainApiResponse> CreateUnsignedChannel([FromBody]CreateChannelModel model)
        {
            var asset = await GetAsset(model.Asset);
            var tr = await _offchain.CreateUnsignedChannel(model.ClientPubKey, model.HotWalletAddress, model.HubAmount, asset
                , model.RequiredOperation, model.TransferId);
            return new OffchainApiResponse(tr);
        }


        [HttpPost("createcashin")]
        [ProducesResponseType(typeof(OffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainApiResponse> CreateCashin([FromBody]CreateCashinModel model)
        {
            var asset = await GetAsset(model.Asset);
            var tr = await _offchain.CreateCashin(model.ClientPubKey, model.Amount, asset, model.CashinAddress, model.TransferId);
            return new OffchainApiResponse(tr);
        }


        [HttpPost("createhubcommitment")]
        [ProducesResponseType(typeof(OffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainApiResponse> CreateHubCommitment([FromBody] CreateHubCommitmentModel model)
        {
            var asset = await GetAsset(model.Asset);
            var tr = await _offchain.CreateHubCommitment(model.ClientPubKey, asset, model.Amount, model.SignedByClientChannel);
            return new OffchainApiResponse(tr);
        }

        [HttpPost("finalize")]
        [ProducesResponseType(typeof(FinalizeOffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<FinalizeOffchainApiResponse> Finalize([FromBody] FinalizeChannelModel model)
        {
            var asset = await GetAsset(model.Asset);
            var tr = await _offchain.Finalize(model.ClientPubKey, model.HotWalletAddress, asset, model.ClientRevokePubKey, model.SignedByClientHubCommitment, model.TransferId);
            return new FinalizeOffchainApiResponse(tr);
        }

        [HttpPost("broadcastcommitment")]
        [ProducesResponseType(typeof(TransactionHashResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<TransactionHashResponse> BroadcastCommitment([FromBody]BroadcastCommitmentModel model)
        {
            var asset = await GetAsset(model.Asset);
            return new TransactionHashResponse(await _offchain.BroadcastCommitment(model.ClientPubKey, asset, model.Transaction));
        }

        [HttpPost("cashout")]
        [ProducesResponseType(typeof(CashoutOffchainApiResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<CashoutOffchainApiResponse> CreateCashout([FromBody]CashoutModel model)
        {
            var asset = await GetAsset(model.Asset);
            return new CashoutOffchainApiResponse(await _offchain.CreateCashout(model.ClientPubKey, model.CashoutAddress,
                model.HotWalletAddress, model.Amount, asset));
        }


        [HttpPost("broadcastclosing")]
        [ProducesResponseType(typeof(TransactionHashResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<TransactionHashResponse> BroadcastClosing([FromBody]BroadcastClosingChannelModel model)
        {
            var asset = await GetAsset(model.Asset);
            return new TransactionHashResponse(await _offchain.BroadcastClosingChannel(model.ClientPubKey, asset, model.SignedByClientTransaction));
        }


        [HttpGet("clientbalance")]
        [ProducesResponseType(typeof(OffchainClientBalanceResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainClientBalanceResponse> GetClientBalance([FromQuery] string multisig, [FromQuery] string asset)
        {
            var assetObj = await GetAsset(asset);
            return new OffchainClientBalanceResponse
            {
                Amount = await _offchain.GetClientBalance(multisig, assetObj)
            };
        }

        [HttpGet("balances")]
        [ProducesResponseType(typeof(OffchainBalanceResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<OffchainBalanceResponse> GetBalances([FromQuery] string multisig)
        {
            return new OffchainBalanceResponse(await _offchain.GetBalances(multisig));
        }

        [HttpPost("removechannel")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task RemoveChannel([FromBody]RemoveChannelModel model)
        {
            var asset = await GetAsset(model.Asset);
            await _offchain.RemoveChannel(model.Multisig, asset);
        }


        private async Task<IAsset> GetAsset(string assetId)
        {
            var asset = await _assetRepository.GetItemAsync(assetId);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);
            return asset;
        }
    }

}
