namespace Core.TransactionQueueWriter
{
    public enum TransactionCommandType
    {
        Issue,
        Transfer,
        TransferAll,
        Swap,        
        Destroy,
        MultipleTransfers
    }
}
