using System;
using MessagePack;

namespace Lykke.Bitcoin.Contracts.Commands
{
    /// <summary>
    /// Cashin command
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class StartCashinCommand
    {
        public Guid Id { get; set; }

        public string Address { get; set; }
    }
}
