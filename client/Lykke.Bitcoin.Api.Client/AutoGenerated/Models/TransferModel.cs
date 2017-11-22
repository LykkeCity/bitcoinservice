// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class TransferModel
    {
        /// <summary>
        /// Initializes a new instance of the TransferModel class.
        /// </summary>
        public TransferModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the TransferModel class.
        /// </summary>
        public TransferModel(string clientPubKey = default(string), decimal? amount = default(decimal?), string asset = default(string), string clientPrevPrivateKey = default(string), bool? requiredOperation = default(bool?), System.Guid? transferId = default(System.Guid?))
        {
            ClientPubKey = clientPubKey;
            Amount = amount;
            Asset = asset;
            ClientPrevPrivateKey = clientPrevPrivateKey;
            RequiredOperation = requiredOperation;
            TransferId = transferId;
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
        [JsonProperty(PropertyName = "amount")]
        public decimal? Amount { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "asset")]
        public string Asset { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "clientPrevPrivateKey")]
        public string ClientPrevPrivateKey { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "requiredOperation")]
        public bool? RequiredOperation { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "transferId")]
        public System.Guid? TransferId { get; set; }

    }
}
