// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class RetryFailedRequest
    {
        /// <summary>
        /// Initializes a new instance of the RetryFailedRequest class.
        /// </summary>
        public RetryFailedRequest()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the RetryFailedRequest class.
        /// </summary>
        public RetryFailedRequest(System.Guid? transactionId = default(System.Guid?))
        {
            TransactionId = transactionId;
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

    }
}
