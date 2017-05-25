using System.Collections.Generic;
using LkeServices.Transactions;

namespace BitcoinApi.Models.Offchain
{
    public class OffchainCommitmentsOfChannelResponse
    {
        public IEnumerable<OffchainCommitmentInfo> Commitments { get; set; }

        public OffchainCommitmentsOfChannelResponse(IEnumerable<OffchainCommitmentInfo> commitments)
        {
            Commitments = commitments;
        }
    }
}
