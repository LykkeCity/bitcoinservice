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
        Exception,
        ProblemInRetrivingWalletOutput,
        ProblemInRetrivingTransaction,
        NotEnoughBitcoinAvailable,
        NotEnoughAssetAvailable,
        PossibleDoubleSpend,
        AssetNotFound,
        TransactionNotSignedProperly,
        BadInputParameter,
        PersistantConcurrencyProblem,
        NoCoinsToRefund,
        NoCoinsFound,
        InvalidAddress,
        OperationNotSupported,
        PregeneratedPoolIsEmpty,
        TransactionConcurrentInputsProblem,
        AddressHasUncompletedSignRequest,
        ShouldOpenNewChannel,
        BadTransaction,
        BadFullSignTransaction,
        CommitmentNotFound,
        DuplicateTransactionId,
        AnotherChannelSetupExists,
        BadChannelAmount,
        ChannelNotFinalized,
        CommitmentExpired
    }
}
