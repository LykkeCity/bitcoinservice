// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Bitcoin.Api.Client.AutoGenerated.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class SegwitWalletResult
    {
        /// <summary>
        /// Initializes a new instance of the SegwitWalletResult class.
        /// </summary>
        public SegwitWalletResult()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the SegwitWalletResult class.
        /// </summary>
        public SegwitWalletResult(string segwitAddress = default(string))
        {
            SegwitAddress = segwitAddress;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "segwitAddress")]
        public string SegwitAddress { get; set; }

    }
}
