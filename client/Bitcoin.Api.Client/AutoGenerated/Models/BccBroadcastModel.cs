// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Bitcoin.Api.Client.AutoGenerated.Models
{
    using Bitcoin.Api;
    using Bitcoin.Api.Client;
    using Bitcoin.Api.Client.AutoGenerated;
    using Newtonsoft.Json;
    using System.Linq;

    public partial class BccBroadcastModel
    {
        /// <summary>
        /// Initializes a new instance of the BccBroadcastModel class.
        /// </summary>
        public BccBroadcastModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the BccBroadcastModel class.
        /// </summary>
        public BccBroadcastModel(string transaction = default(string))
        {
            Transaction = transaction;
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

    }
}
