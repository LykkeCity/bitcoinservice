namespace Core.TransactionQueueWriter.Commands
{
    public class TransferCommand
    {        
        public string SourceAddress { get; set; }

        public string DestinationAddress { get; set; }

        public decimal Amount { get; set; }

        public string Asset { get; set; }
    }
}
