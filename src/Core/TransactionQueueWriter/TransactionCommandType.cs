namespace Core.TransactionQueueWriter
{
    public enum TransactionCommandType
    {
        Issue = 0,
        Transfer = 1,
        TransferAll = 2,
        Swap = 3,
        Destroy = 4,
        SegwitTransferToHotwallet = 5
    }
}
