using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Repositories.MultipleCashouts
{
    public interface IMultipleCashout
    {
        Guid MultipleCashoutId { get; }

        string TransactionHash { get; }

        string TransactionHex { get; }
        int TryCount { get; }

        MultiCashoutState State { get; }
    }

    public enum MultiCashoutState
    {
        Open = 1,
        Closed = 2,
        Completed = 3
    }
}
