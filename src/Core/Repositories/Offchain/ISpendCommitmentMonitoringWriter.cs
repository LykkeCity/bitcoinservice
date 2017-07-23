using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.Offchain
{
    public interface ISpendCommitmentMonitoringWriter
    {
        Task AddToMonitoring(Guid commitmentId, string transactionHash);
    }


    public class SpendCommitmentMonitorindMessage
    {
        public DateTime PutDateTime { get; set; }

        public Guid CommitmentId { get; set; }

        public string TransactionHash { get; set; }
    }
}
