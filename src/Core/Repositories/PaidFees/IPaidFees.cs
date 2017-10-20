using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.PaidFees
{
    public interface IPaidFees
    {
        string TransactionHash { get; set; }

        decimal Amount { get; set; }

        DateTime Date { get; set; }

        string Multisig { get; set; }

        string Asset { get; set; }
    }

    public interface IPaidFeesRepository
    {
        Task Insert(string hash, decimal amount, DateTime date, string multisig, string asset);
    }
}
