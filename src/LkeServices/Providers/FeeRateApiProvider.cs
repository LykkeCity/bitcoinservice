using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Providers;
using RestSharp;

namespace LkeServices.Providers
{
    public class FeeRateApiProvider : BaseApiProvider, IFeeRateApiProvider
    {
        const string Url = "https://bitcoinfees.21.co/api/v1/fees/recommended";

        public FeeRateApiProvider(IRestClient restClient, ILog logger)
           : base(Url, restClient, logger)
        {
        }

        public async Task<FeeResult> GetFee()
        {
            var request = new RestRequest(Method.GET);

            return await DoRequest<FeeResult>(request);
        }
    }
}
