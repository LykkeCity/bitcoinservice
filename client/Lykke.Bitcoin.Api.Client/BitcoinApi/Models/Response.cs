namespace Lykke.Bitcoin.Api.Client.BitcoinApi.Models
{
    public class Response
    {
        public ErrorResponse Error { get; set; }

        public bool HasError => Error != null;
    }
}
