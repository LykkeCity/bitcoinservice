using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Notifiers
{
    public interface ISlackNotifier
    {
        Task WarningAsync(string message);
        Task ErrorAsync(string message);
    }
}
