// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

using Newtonsoft.Json;

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    public partial class RemoveChannelModel
    {
        /// <summary>
        /// Initializes a new instance of the RemoveChannelModel class.
        /// </summary>
        public RemoveChannelModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the RemoveChannelModel class.
        /// </summary>
        public RemoveChannelModel(string multisig = default(string), string asset = default(string))
        {
            Multisig = multisig;
            Asset = asset;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "multisig")]
        public string Multisig { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "asset")]
        public string Asset { get; set; }

    }
}
