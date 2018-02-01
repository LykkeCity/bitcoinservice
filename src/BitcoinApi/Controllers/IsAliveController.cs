using System;
using System.Net;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Providers;
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
        private readonly ISignatureApi _signatureApi;

        public IsAliveController(IRpcBitcoinClient rpcClient, Func<QBitNinjaClient> qbitninja, ISignatureApi signatureApi)
        {
            _rpcClient = rpcClient;
            _qbitninja = qbitninja;
            _signatureApi = signatureApi;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {          
            var serverUp = await CheckSigninService();
            
            if (!serverUp)
                return StatusCode((int)HttpStatusCode.InternalServerError, new ErrorResponse
                {
                    ErrorMessage = $"Job is unhealthy: Server signing service is not working!"
                });

            return Ok(new IsAliveResponse()
            {
                Name = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationName,
                Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                Env = Environment.GetEnvironmentVariable("ENV_INFO")
            });
        }

        private async Task<bool> CheckSigninService()
        {
            try
            {
                await _signatureApi.IsAlive();
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

        public class ErrorResponse
        {
            public string ErrorMessage { get; set; }
        }

        public class IsAliveResponse
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string Env { get; set; }
            public bool IsDebug { get; set; }
        }
    }
}
