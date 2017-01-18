namespace Core.TransactionQueueWriter.Commands
{
    public class IssueCommand
    {        
        public string Address { get; set; }
        public string Asset { get; set; }
        public decimal Amount { get; set; }
    }
}
