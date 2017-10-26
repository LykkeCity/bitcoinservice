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

    public partial class ToOneAddress
    {
        /// <summary>
        /// Initializes a new instance of the ToOneAddress class.
        /// </summary>
        public ToOneAddress()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ToOneAddress class.
        /// </summary>
        public ToOneAddress(string address = default(string), decimal? amount = default(decimal?))
        {
            Address = address;
            Amount = amount;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public decimal? Amount { get; set; }

    }
}