using System.Threading.Tasks;
using BitcoinApi.Models.Offchain;
using LkeServices.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class OffchainController : Controller
    {
        private readonly IOffchainTransactionBuilderService _offchainTransactionBuilder;

        public OffchainController(IOffchainTransactionBuilderService offchainTransactionBuilder)
        {
            _offchainTransactionBuilder = offchainTransactionBuilder;
        }

        [HttpPost("createchannel")]
        public async Task<OffchainResponse> CreateUnsignedChannel([FromBody]CreateChannelModel model)
        {
            var tr = await _offchainTransactionBuilder.CreateUnsignedChannel(model.ClientPubKey, model.HotWalletPubKey,
                model.ClientAmount, model.HubAmount, model.Asset);
            return new OffchainResponse(tr);
        }

        [HttpPost("createhubcommitment")]
        public async Task<OffchainResponse> CreateHubCommitment([FromBody] CreateHubCommitmentModel model)
        {
            var tr = await _offchainTransactionBuilder.CreateHubCommitment(model.ClientPubKey, model.Asset,
                model.ClientAmount, model.HubAmount, model.SignedByClientChannel);
            return new OffchainResponse(tr);
        }

        [HttpPost("finalizechannel")]
        public async Task<OffchainResponse> FinalizeChannel([FromBody] FinalizeChannelModel model)
        {
            var tr = await _offchainTransactionBuilder.FinalizeChannel(model.ClientPubKey, model.HotWalletPubKey,
                model.Asset, model.ClientRevokePubKey, model.SignedByClientHubCommitment);
            return new OffchainResponse(tr);
        }
    }

}
