// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using Newtonsoft.Json;

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    public partial class OffchainCommitmentInfo
    {
        /// <summary>
        /// Initializes a new instance of the OffchainCommitmentInfo class.
        /// </summary>
        public OffchainCommitmentInfo()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the OffchainCommitmentInfo class.
        /// </summary>
        public OffchainCommitmentInfo(System.DateTime? date = default(System.DateTime?), decimal? clientAmount = default(decimal?), decimal? hubAmount = default(decimal?), System.Guid? clientCommitment = default(System.Guid?), System.Guid? hubCommitment = default(System.Guid?))
        {
            Date = date;
            ClientAmount = clientAmount;
            HubAmount = hubAmount;
            ClientCommitment = clientCommitment;
            HubCommitment = hubCommitment;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "date")]
        public System.DateTime? Date { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "clientAmount")]
        public decimal? ClientAmount { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hubAmount")]
        public decimal? HubAmount { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "clientCommitment")]
        public System.Guid? ClientCommitment { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hubCommitment")]
        public System.Guid? HubCommitment { get; set; }

    }
}
