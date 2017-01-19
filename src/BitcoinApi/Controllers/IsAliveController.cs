using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
using Microsoft.AspNetCore.Mvc;
using QBitNinja.Client;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly IRpcBitcoinClient _rpcClient;
        private readonly Func<QBitNinjaClient> _qbitninja;

        public IsAliveController(IRpcBitcoinClient rpcClient, Func<QBitNinjaClient> qbitninja)
        {
            _rpcClient = rpcClient;
            _qbitninja = qbitninja;
        }

        [HttpGet]
        public async Task<IsAliveResponse> Get()
        {
            await _rpcClient.GetBlockCount();

            await _qbitninja().GetBlock(new QBitNinja.Client.Models.BlockFeature(1));

            return new IsAliveResponse
            {
                Version =
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion
            };
        }

        public class IsAliveResponse
        {
            public string Version { get; set; }
        }
    }
}
