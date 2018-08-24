using MessagePack;
using System;

namespace Lykke.Bitcoin.Contracts.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashinCompletedEvent
    {
        public Guid OperationId { get; set; }

        public string TxHash { get; set; }
    }
}