namespace Core.TransactionQueueWriter.Commands
{
    public class SwapCommand
    {        
        public string MultisigCustomer1 { get; set; }

        public decimal Amount1 { get; set; }

        public string Asset1 { get; set; }

        public string MultisigCustomer2 { get; set; }

        public decimal Amount2 { get; set; }

        public string Asset2 { get; set; }
    }
}
