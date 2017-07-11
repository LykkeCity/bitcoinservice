using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Settings;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Settings
{

    public class SettingsEntity : BaseEntity
    {
        public static string GeneratePartitionKey()
        {
            return "Settings";
        }

        public string Value { get; set; }

        public SettingsEntity(string key, string value)
        {
            RowKey = key;
            Value = value;
            PartitionKey = GeneratePartitionKey();
        }

        public SettingsEntity()
        {

        }
    }

    public class SettingsRepository : ISettingsRepository
    {
        private readonly INoSQLTableStorage<SettingsEntity> _table;

        public SettingsRepository(INoSQLTableStorage<SettingsEntity> table)
        {
            _table = table;
        }
        public async Task<T> Get<T>(string key)
        {
            var setting = await _table.GetDataAsync(SettingsEntity.GeneratePartitionKey(), key);
            if (setting == null)
                return default(T);
            return (T)Convert.ChangeType(setting.Value, typeof(T));
        }

        public Task<T> Get<T>(string key, T defaultValue)
        {
            throw new NotImplementedException();
        }      

        public Task Set<T>(string key, T value)
        {
            return _table.InsertOrReplaceAsync(new SettingsEntity(key, value?.ToString()));
        }
    }
}
