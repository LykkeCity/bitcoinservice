// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using Newtonsoft.Json;

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    public partial class OffchainApiResponse
    {
        /// <summary>
        /// Initializes a new instance of the OffchainApiResponse class.
        /// </summary>
        public OffchainApiResponse()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the OffchainApiResponse class.
        /// </summary>
        public OffchainApiResponse(string transaction = default(string), System.Guid? transferId = default(System.Guid?))
        {
            Transaction = transaction;
            TransferId = transferId;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "transaction")]
        public string Transaction { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "transferId")]
        public System.Guid? TransferId { get; set; }

    }
}
