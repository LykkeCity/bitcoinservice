using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Repositories.MultipleCashouts
{
    public interface ICashoutRequest
    {
        Guid CashoutRequestId { get; }

        decimal Amount { get; }
        
        string DestinationAddress { get; }

        DateTime Date { get; }

        Guid? MultipleCashoutId { get; }        
    }   
}
