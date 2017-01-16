using System.Threading.Tasks;
using BitcoinApi.Models;
using Core.Bitcoin;
using Core.Providers;
using LkeServices.Multisig;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class WalletController : Controller
    {
        private readonly IMultisigService _multisigService;

        public WalletController(IMultisigService multisigService)
        {
            _multisigService = multisigService;
        }

        /// <summary>
        /// Returns 2-of-2 multisig with exchange key and provided public key
        /// </summary>
        /// <param name="clientPubKey">Client public key</param>
        /// <remarks>
        /// curl -X GET http://localhost:8989/api/wallet/&lt;client_public_key&gt; 
        /// </remarks>
        /// <returns>Multisig address and colored (OpenAssets) representation</returns>
        [HttpGet("{clientPubKey}")]
        public async Task<GetWalletResult> GetWallet(string clientPubKey)
        {
            var address = await _multisigService.GetOrCreateMultisig(clientPubKey);

            var coloredMultisigAddress = BitcoinAddress.Create(address.MultisigAddress).ToColoredAddress().ToWif();

            return new GetWalletResult
            {
                MultiSigAddress = address.MultisigAddress,
                ColoredMultiSigAddress = coloredMultisigAddress
            };
        }
    }
}
