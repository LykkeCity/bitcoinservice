using System;

namespace Core.Exceptions
{
    public class BackendException : Exception
    {
        public ErrorCode Code { get; private set; }
        public string Text { get; private set; }

        public BackendException(string text, ErrorCode code)
            : base(text)
        {
            Code = code;
            Text = text;
        }
    }

    public enum ErrorCode
    {
        Exception = 0,
        ProblemInRetrivingWalletOutput = 1,
        ProblemInRetrivingTransaction = 2,
        NotEnoughBitcoinAvailable = 3,
        NotEnoughAssetAvailable = 4,
        PossibleDoubleSpend = 5,
        AssetNotFound = 6,
        TransactionNotSignedProperly = 7,
        BadInputParameter = 8,
        PersistantConcurrencyProblem = 9,
        NoCoinsToRefund = 10,
        NoCoinsFound = 11,
        InvalidAddress = 12,
        OperationNotSupported = 13,
        PregeneratedPoolIsEmpty = 14,
        TransactionConcurrentInputsProblem = 15,
        AddressHasUncompletedSignRequest = 16,
        ShouldOpenNewChannel = 17,
        BadTransaction = 18,
        BadFullSignTransaction = 19,
        CommitmentNotFound = 20,
        DuplicateTransactionId = 21,
        AnotherChannelSetupExists = 22,
        BadChannelAmount = 23,
        ChannelNotFinalized = 24,
        CommitmentExpired = 25,
        KeyUsedAlready = 26,
        NotEnoughtClientFunds = 27,
        PrivateKeyIsBad = 28,
        ClosingChannelNotFound = 29,
        ClosingChannelExpired = 30,
        DuplicateRequest = 31,
        TransferNotFound = 32,
        WrongTransferId = 33
    }
}
