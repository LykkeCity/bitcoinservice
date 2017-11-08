// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using Newtonsoft.Json;

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    public partial class SwapRequest
    {
        /// <summary>
        /// Initializes a new instance of the SwapRequest class.
        /// </summary>
        public SwapRequest()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the SwapRequest class.
        /// </summary>
        public SwapRequest(System.Guid? transactionId = default(System.Guid?), string multisigCustomer1 = default(string), decimal? amount1 = default(decimal?), string asset1 = default(string), string multisigCustomer2 = default(string), decimal? amount2 = default(decimal?), string asset2 = default(string))
        {
            TransactionId = transactionId;
            MultisigCustomer1 = multisigCustomer1;
            Amount1 = amount1;
            Asset1 = asset1;
            MultisigCustomer2 = multisigCustomer2;
            Amount2 = amount2;
            Asset2 = asset2;
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
        [JsonProperty(PropertyName = "multisigCustomer1")]
        public string MultisigCustomer1 { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "amount1")]
        public decimal? Amount1 { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "asset1")]
        public string Asset1 { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "multisigCustomer2")]
        public string MultisigCustomer2 { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "amount2")]
        public decimal? Amount2 { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "asset2")]
        public string Asset2 { get; set; }

    }
}
