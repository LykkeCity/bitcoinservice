using System;
using MessagePack;

namespace Lykke.Bitcoin.Contracts.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashoutCompletedEvent
    {
        public Guid OperationId { get; set; }
    }
}