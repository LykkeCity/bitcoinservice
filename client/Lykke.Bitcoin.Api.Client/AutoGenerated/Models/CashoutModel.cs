// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using Newtonsoft.Json;

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    public partial class CashoutModel
    {
        /// <summary>
        /// Initializes a new instance of the CashoutModel class.
        /// </summary>
        public CashoutModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the CashoutModel class.
        /// </summary>
        public CashoutModel(string clientPubKey = default(string), string cashoutAddress = default(string), string hotWalletAddress = default(string), string asset = default(string), decimal? amount = default(decimal?))
        {
            ClientPubKey = clientPubKey;
            CashoutAddress = cashoutAddress;
            HotWalletAddress = hotWalletAddress;
            Asset = asset;
            Amount = amount;
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
        [JsonProperty(PropertyName = "cashoutAddress")]
        public string CashoutAddress { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hotWalletAddress")]
        public string HotWalletAddress { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "asset")]
        public string Asset { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public decimal? Amount { get; set; }

    }
}