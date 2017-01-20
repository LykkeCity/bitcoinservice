using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using RestSharp;
using Common;
using Core.Repositories.ApiRequests;
using Core.Settings;

namespace LkeServices.Providers
{
    public class BaseApiProvider
    {
        private int _requestId;

        private readonly IRestClient _restClient;
        private readonly IApiRequestBlobRepository _apiRequestRepository;
        protected readonly ILog Logger;

        public BaseApiProvider(string url, IRestClient restClient, ILog logger, IApiRequestBlobRepository apiRequestRepository)
        {
            _restClient = restClient;
            Logger = logger;
            _apiRequestRepository = apiRequestRepository;
            _restClient.BaseUrl = new Uri(url);
        }


        public async Task<T> DoRequest<T>(RestRequest request) where T : new()
        {
            var requestGuid = Guid.NewGuid();

            var reqId = Interlocked.Increment(ref _requestId);

            var requestLog = $"Request reqId={reqId}, Method: {request.Method} {request.Resource}, Guid: {requestGuid}";

            var info = new StringBuilder();
            info.AppendLine(requestLog);
            foreach (var parameter in request.Parameters)
                info.Append(parameter.Name + "=" + parameter.Value + Environment.NewLine);

            await _apiRequestRepository.LogToBlob(requestGuid, "request", info.ToString());

            await Logger.WriteInfoAsync("ApiCaller", "DoRequest", "", requestLog);

            var t = new TaskCompletionSource<IRestResponse>();
            _restClient.ExecuteAsync(request, resp => { t.SetResult(resp); });
            var response = await t.Task;

            if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK)
            {
                var content = response.Content;

                await Logger.WriteInfoAsync("ApiCaller", "DoRequest", "", $"Response reqId={reqId}, guid: {requestGuid}");

                await _apiRequestRepository.LogToBlob(requestGuid, "response", content);

                return string.IsNullOrWhiteSpace(content) ? default(T) : content.DeserializeJson<T>();
            }
            var exception = response.ErrorException ?? new Exception(response.Content);
            await Logger.WriteErrorAsync("ApiCaller", "DoRequest", $"reqId={reqId}, guid: {requestGuid}", exception);
            throw exception;
        }
    }
}
