// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class TransferRequest
    {
        /// <summary>
        /// Initializes a new instance of the TransferRequest class.
        /// </summary>
        public TransferRequest()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the TransferRequest class.
        /// </summary>
        public TransferRequest(System.Guid? transactionId = default(System.Guid?), string sourceAddress = default(string), string destinationAddress = default(string), decimal? amount = default(decimal?), string asset = default(string))
        {
            TransactionId = transactionId;
            SourceAddress = sourceAddress;
            DestinationAddress = destinationAddress;
            Amount = amount;
            Asset = asset;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "transactionId")]
        public System.Guid? TransactionId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "sourceAddress")]
        public string SourceAddress { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "destinationAddress")]
        public string DestinationAddress { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public decimal? Amount { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "asset")]
        public string Asset { get; set; }

    }
}
