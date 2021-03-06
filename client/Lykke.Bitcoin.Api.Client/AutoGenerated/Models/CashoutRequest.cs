// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class CashoutRequest
    {
        /// <summary>
        /// Initializes a new instance of the CashoutRequest class.
        /// </summary>
        public CashoutRequest()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the CashoutRequest class.
        /// </summary>
        public CashoutRequest(System.Guid? transactionId = default(System.Guid?), string destinationAddress = default(string), string asset = default(string), decimal? amount = default(decimal?))
        {
            TransactionId = transactionId;
            DestinationAddress = destinationAddress;
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
        [JsonProperty(PropertyName = "transactionId")]
        public System.Guid? TransactionId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "destinationAddress")]
        public string DestinationAddress { get; set; }

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
