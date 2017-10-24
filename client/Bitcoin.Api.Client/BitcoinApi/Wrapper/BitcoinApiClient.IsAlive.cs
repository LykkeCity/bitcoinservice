using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Bitcoin.Api.Client.BitcoinApi.Models;
using Microsoft.Rest;

// ReSharper disable once CheckNamespace
namespace Bitcoin.Api.Client.BitcoinApi
{
    public partial class BitcoinApiClient
    {
        public async Task<HttpOperationResponse> IsAlive()
        {
            return await _apiClient.ApiIsAliveGetWithHttpMessagesAsync();
        }

        public async Task<HttpOperationResponse> IsAliveRpc()
        {
            return await _apiClient.ApiIsAliveRpcGetWithHttpMessagesAsync();
        }

        public async Task<HttpOperationResponse> IsAliveNinja()
        {
            return await _apiClient.ApiIsAliveNinjaGetWithHttpMessagesAsync();
        }
    }
}
