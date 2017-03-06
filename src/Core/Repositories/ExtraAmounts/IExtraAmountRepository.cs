using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.ExtraAmounts
{
    public interface IExtraAmount
    {
        string Address { get; }
        long Amount { get; }
    }

    public interface IExtraAmountRepository
    {
        Task<IExtraAmount> Add(string address, long amount);
        Task Decrease(IExtraAmount extraAmount);
    }
}
