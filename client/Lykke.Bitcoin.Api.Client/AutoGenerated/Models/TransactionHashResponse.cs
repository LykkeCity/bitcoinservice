// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class TransactionHashResponse
    {
        /// <summary>
        /// Initializes a new instance of the TransactionHashResponse class.
        /// </summary>
        public TransactionHashResponse()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the TransactionHashResponse class.
        /// </summary>
        public TransactionHashResponse(string transactionHash = default(string))
        {
            TransactionHash = transactionHash;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "transactionHash")]
        public string TransactionHash { get; set; }

    }
}
