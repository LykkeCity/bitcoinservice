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
using Core.Settings;

namespace LkeServices.Providers
{
    public class BaseApiProvider
    {
        private int _requestId;

        private readonly IRestClient _restClient;
        protected readonly ILog Logger;

        public BaseApiProvider(string url, IRestClient restClient, ILog logger)
        {
            _restClient = restClient;
            Logger = logger;
            _restClient.BaseUrl = new Uri(url);
        }


        public async Task<T> DoRequest<T>(RestRequest request) where T : new()
        {
            var reqId = Interlocked.Increment(ref _requestId);

            var info = new StringBuilder();
            info.Append($"Invoke request reqId={reqId}, Method: {request.Method} {request.Resource}, Params: {Environment.NewLine}");
            foreach (var parameter in request.Parameters)
                info.Append(parameter.Name + "=" + parameter.Value + Environment.NewLine);
            await Logger.WriteInfoAsync("ApiCaller", "DoRequest", "", info.ToString());

            var t = new TaskCompletionSource<IRestResponse>();
            _restClient.ExecuteAsync(request, resp => { t.SetResult(resp); });
            var response = await t.Task;

            if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK)
            {
                var content = response.Content;
                await Logger.WriteInfoAsync("ApiCaller", "DoRequest", "", $"Response reqId={reqId}: {content} ");
                return string.IsNullOrWhiteSpace(response.Content) ? default(T) : response.Content.DeserializeJson<T>();
            }
            var exception = response.ErrorException ?? new Exception(response.Content);
            await Logger.WriteErrorAsync("ApiCaller", "DoRequest", $"reqId={reqId}", exception);
            throw exception;
        }
    }
}
