using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace BitcoinJob.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class StartCashoutCommand
    {
        public Guid Id { get; set; }

        public decimal Amount { get; set; }

        public string Address { get; set; }

        public string AssetId { get; set; }
    }
}
