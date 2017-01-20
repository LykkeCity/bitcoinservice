using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Settings;
using Microsoft.AspNetCore.Mvc;
using QBitNinja.Client;
using RestSharp;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly IRpcBitcoinClient _rpcClient;
        private readonly Func<QBitNinjaClient> _qbitninja;
        private readonly IRestClient _client;
        private readonly BaseSettings _settings;

        public IsAliveController(IRpcBitcoinClient rpcClient, Func<QBitNinjaClient> qbitninja, IRestClient client, BaseSettings settings)
        {
            _rpcClient = rpcClient;
            _qbitninja = qbitninja;
            _client = client;
            _settings = settings;
        }

        [HttpGet]
        public async Task<IsAliveResponse> Get()
        {
            await _rpcClient.GetBlockCount();

            await _qbitninja().GetBlock(new QBitNinja.Client.Models.BlockFeature(1));

            await GetSignatureAlive(_settings.ClientSignatureProviderUrl);

            await GetSignatureAlive(_settings.SignatureProviderUrl);

            return new IsAliveResponse
            {
                Version =
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion
            };
        }


        private async Task GetSignatureAlive(string baseUrl)
        {
            _client.BaseUrl = new Uri(baseUrl);
            var request = new RestRequest("/api/IsAlive");
            var t = new TaskCompletionSource<IRestResponse>();
            _client.ExecuteAsync(request, resp => { t.SetResult(resp); });
            var response = await t.Task;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"Signature: {baseUrl} is down");
        }

        public class IsAliveResponse
        {
            public string Version { get; set; }
        }
    }
}
