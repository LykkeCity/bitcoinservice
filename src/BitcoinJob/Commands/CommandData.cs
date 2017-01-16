using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackgroundWorker.Commands
{
    public enum CommandType
    {
        GenerateFeeOutputs = 1
    }


    public class CommandData
    {
        public CommandType Type { get; set; }
        
        public string Data { get; set; }
    }
}
