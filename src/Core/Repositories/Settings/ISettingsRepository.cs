using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.Settings
{
    public interface ISettingsRepository
    {
        Task<T> Get<T>(string key);
        Task<T> Get<T>(string key, T defaultValue);
        Task<T> Set<T>(string key, T value);
    }
}
