namespace Core.TransactionQueueWriter.Commands
{
    public class TransferAllCommand
    {        
        public string SourceAddress { get; set; }

        public string DestinationAddress { get; set; }
    }
}
