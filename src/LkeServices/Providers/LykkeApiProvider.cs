using System;
using System.Threading.Tasks;
using Common.Log;
using Core.Providers;
using Core.Settings;
using RestSharp;

namespace LkeServices.Providers
{
    public class LykkeApiProvider : BaseApiProvider, ILykkeApiProvider
    {
        public LykkeApiProvider(BaseSettings settings, IRestClient restClient, ILog logger)
            : base(settings.LykkeJobsUrl, restClient, logger)
        {
        }

        public async Task SendPreBroadcastNotification(Guid transactionId, string transactionHash)
        {
            try
            {
                var request = new RestRequest("/api/PreBroadcastNotification", Method.POST);

                request.AddJsonBody(new
                {
                    TransactionId = transactionId,
                    TransactionHash = transactionHash
                });

                await DoRequest<SuccessResponse>(request);
            }
            catch (Exception e)
            {
                await Logger.WriteErrorAsync("LykkeApiProvider", "SendPostBroadcastNotification", $"TransactionId: {transactionId}, hash: {transactionHash}", e);
                throw;
            }
        }

        public async Task SendPostBroadcastNotification(Guid transactionId, string transactionHash)
        {
            try
            {
                var request = new RestRequest("/api/PostBroadcastNotification", Method.POST);

                request.AddJsonBody(new
                {
                    TransactionId = transactionId,
                    TransactionHash = transactionHash
                });

                await DoRequest<SuccessResponse>(request);
            }
            catch (Exception e)
            {
                await Logger.WriteErrorAsync("LykkeApiProvider", "SendPostBroadcastNotification", $"TransactionId: {transactionId}, hash: {transactionHash}", e);
            }
        }
    }

    public class SuccessResponse
    {

    }
}
