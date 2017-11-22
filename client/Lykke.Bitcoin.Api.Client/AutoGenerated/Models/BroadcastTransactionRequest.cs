// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class BroadcastTransactionRequest
    {
        /// <summary>
        /// Initializes a new instance of the BroadcastTransactionRequest
        /// class.
        /// </summary>
        public BroadcastTransactionRequest()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the BroadcastTransactionRequest
        /// class.
        /// </summary>
        public BroadcastTransactionRequest(System.Guid? transactionId = default(System.Guid?), string transaction = default(string))
        {
            TransactionId = transactionId;
            Transaction = transaction;
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
        [JsonProperty(PropertyName = "transaction")]
        public string Transaction { get; set; }

    }
}
