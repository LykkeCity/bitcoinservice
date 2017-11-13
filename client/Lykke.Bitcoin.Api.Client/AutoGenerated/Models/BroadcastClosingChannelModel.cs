// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using Newtonsoft.Json;

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    public partial class BroadcastClosingChannelModel
    {
        /// <summary>
        /// Initializes a new instance of the BroadcastClosingChannelModel
        /// class.
        /// </summary>
        public BroadcastClosingChannelModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the BroadcastClosingChannelModel
        /// class.
        /// </summary>
        public BroadcastClosingChannelModel(string clientPubKey = default(string), string asset = default(string), string signedByClientTransaction = default(string), System.Guid? notifyTxId = default(System.Guid?))
        {
            ClientPubKey = clientPubKey;
            Asset = asset;
            SignedByClientTransaction = signedByClientTransaction;
            NotifyTxId = notifyTxId;
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
        [JsonProperty(PropertyName = "asset")]
        public string Asset { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "signedByClientTransaction")]
        public string SignedByClientTransaction { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "notifyTxId")]
        public System.Guid? NotifyTxId { get; set; }

    }
}