using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Settings
{
    public class GeneralSettings
    {
        public BaseSettings BitcoinApi { get; set; }
        public BaseSettings BitcoinJobs { get; set; }
    }
}
