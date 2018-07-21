using System;
using MessagePack;

namespace Lykke.Bitcoin.Contracts.Commands
{
    /// <summary>
    /// Cashout command
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class StartCashoutCommand
    {
        public Guid Id { get; set; }

        public decimal Amount { get; set; }

        public string Address { get; set; }

        public string AssetId { get; set; }
    }
}
