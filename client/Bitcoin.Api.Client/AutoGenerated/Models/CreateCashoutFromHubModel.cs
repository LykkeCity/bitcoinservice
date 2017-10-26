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

    public partial class CreateCashoutFromHubModel
    {
        /// <summary>
        /// Initializes a new instance of the CreateCashoutFromHubModel class.
        /// </summary>
        public CreateCashoutFromHubModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the CreateCashoutFromHubModel class.
        /// </summary>
        public CreateCashoutFromHubModel(string clientPubKey = default(string), string hotWalletAddress = default(string), string asset = default(string))
        {
            ClientPubKey = clientPubKey;
            HotWalletAddress = hotWalletAddress;
            Asset = asset;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "clientPubKey")]
        public string ClientPubKey { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hotWalletAddress")]
        public string HotWalletAddress { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "asset")]
        public string Asset { get; set; }

    }
}
