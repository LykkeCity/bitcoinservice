using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace BitcoinJob.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class StartCashinCommand
    {
        public Guid Id { get; set; }

        public string Address { get; set; }
    }
}
