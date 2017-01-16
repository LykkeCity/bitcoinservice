using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackgroundWorker.Handlers
{
    public interface IHandler
    {
        Task Execute(string command);
    }
}
