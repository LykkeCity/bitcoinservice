using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Repositories.ApiRequests;
using RestEase;

namespace LkeServices.Providers.Rest
{
    public class LykkeHttpClientHandler : HttpClientHandler
    {
        private static int _requestId;

        private readonly ILog _logger;
        private readonly IApiRequestBlobRepository _apiRequestRepository;

        public LykkeHttpClientHandler(ILog logger, IApiRequestBlobRepository apiRequestRepository)
        {            
            _logger = logger;
            _apiRequestRepository = apiRequestRepository;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestGuid = Guid.NewGuid();

            var reqId = Interlocked.Increment(ref _requestId);

            //var requestLog = $"Request reqId={reqId}, Method: {request.Method} {request.RequestUri}, Guid: {requestGuid}";

            //var info = new StringBuilder();
            //info.AppendLine(requestLog);
                        
            //if (request.Content != null)
            //    info.AppendLine("Content=" + await request.Content.ReadAsStringAsync());

            //await _apiRequestRepository.LogToBlob(requestGuid, "request", info.ToString());

            //await _logger.WriteInfoAsync("LykkeHttpClientHandler", "SendAsync", "", requestLog);            
            var response = await base.SendAsync(request, cancellationToken);
            try
            {
                response.EnsureSuccessStatusCode();

                //var content = await response.Content.ReadAsStringAsync();
                //await _logger.WriteInfoAsync("LykkeHttpClientHandler", "SendAsync", "", $"Response reqId={reqId}, guid: {requestGuid}");

                //await _apiRequestRepository.LogToBlob(requestGuid, "response", content);                
                return response;
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync("LykkeHttpClientHandler", "SendAsync", $"reqId={reqId}, guid: {requestGuid}", ex);
                throw;
            }
        }
    }
}
