using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinApi.Filters;
using Core.Bitcoin;
using Core.Exceptions;
using Core.Providers;
using Core.Settings;
using LkeServices.Providers;
using Microsoft.AspNetCore.Mvc;
using QBitNinja.Client;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly IRpcBitcoinClient _rpcClient;
        private readonly Func<QBitNinjaClient> _qbitninja;
        private readonly Func<SignatureApiProviderType, ISignatureApi> _signatureApiFactory;

        public IsAliveController(IRpcBitcoinClient rpcClient, Func<QBitNinjaClient> qbitninja, Func<SignatureApiProviderType, ISignatureApi> signatureApiFactory)
        {
            _rpcClient = rpcClient;
            _qbitninja = qbitninja;
            _signatureApiFactory = signatureApiFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (!await CheckSigninService(SignatureApiProviderType.Client))
                return BadRequest(new { Message = "Client signin service is down" });

            if (!await CheckSigninService(SignatureApiProviderType.Exchange))
                return BadRequest(new { Message = "Server signin service is down" });

            return Ok(new IsAliveResponse
            {
                Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion
            });
        }

        private async Task<bool> CheckSigninService(SignatureApiProviderType type)
        {
            try
            {
                await _signatureApiFactory(type).IsAlive();
                return true;
            }
            catch
            {
                // ignored
            }
            return false;
        }

        [HttpGet("rpc")]
        public async Task RpcAlive()
        {
            await _rpcClient.GetBlockCount();
        }

        [HttpGet("ninja")]
        public async Task NinjaAlive()
        {
            await _qbitninja().GetBlock(new QBitNinja.Client.Models.BlockFeature(1));
        }

        public class IsAliveResponse
        {
            public string Version { get; set; }
        }
    }
}
