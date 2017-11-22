using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.PaidFees
{
    public interface IPaidFeesTaskWriter
    {
        Task AddTask(string hash, DateTime date, string asset, string multisig);
    }

    public class PaidFeesTask
    {
        public const int MaxTryCount = 20;

        public string TransactionHash { get; set; }

        public DateTime Date { get; set; }

        public string Asset { get; set; }

        public string Multisig { get; set; }

        public int TryCount { get; set; }
    }
}
