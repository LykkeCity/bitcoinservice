// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Bitcoin.Api.Client.AutoGenerated.Models
{
    using Bitcoin.Api;
    using Bitcoin.Api.Client;
    using Bitcoin.Api.Client.AutoGenerated;
    using Newtonsoft.Json;
    using System.Linq;

    public partial class SplitTransactionResponse
    {
        /// <summary>
        /// Initializes a new instance of the SplitTransactionResponse class.
        /// </summary>
        public SplitTransactionResponse()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the SplitTransactionResponse class.
        /// </summary>
        public SplitTransactionResponse(string transaction = default(string), string outputs = default(string), decimal? clientAmount = default(decimal?), decimal? hubAmount = default(decimal?), decimal? clientFeeAmount = default(decimal?))
        {
            Transaction = transaction;
            Outputs = outputs;
            ClientAmount = clientAmount;
            HubAmount = hubAmount;
            ClientFeeAmount = clientFeeAmount;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "transaction")]
        public string Transaction { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "outputs")]
        public string Outputs { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "clientAmount")]
        public decimal? ClientAmount { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hubAmount")]
        public decimal? HubAmount { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "clientFeeAmount")]
        public decimal? ClientFeeAmount { get; set; }

    }
}
