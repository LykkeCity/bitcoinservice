namespace Core.TransactionQueueWriter.Commands
{
    public class DestroyCommand
    {        
        public string Address { get; set; }
        public string Asset { get; set; }
        public decimal Amount { get; set; }
    }
}
