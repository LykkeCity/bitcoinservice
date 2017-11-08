using System.Threading.Tasks;
using Microsoft.Rest;

// ReSharper disable once CheckNamespace
namespace Lykke.Bitcoin.Api.Client.BitcoinApi
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
