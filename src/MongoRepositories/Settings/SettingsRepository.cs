using System;
using System.Threading.Tasks;
using Core.Repositories.Settings;
using MongoRepositories.Mongo;

namespace MongoRepositories.Settings
{

    public class SettingsEntity : MongoEntity
    {
        public string Value { get; set; }

        public SettingsEntity(string key, string value)
        {
            BsonId = key;
            Value = value;
        }

        public SettingsEntity()
        {

        }
    }

    public class SettingsRepository : ISettingsRepository
    {
        private readonly IMongoStorage<SettingsEntity> _table;

        public SettingsRepository(IMongoStorage<SettingsEntity> table)
        {
            _table = table;
        }
        public async Task<T> Get<T>(string key)
        {
            var setting = await _table.GetDataAsync(key);
            if (setting == null)
                return default(T);
            return (T)Convert.ChangeType(setting.Value, typeof(T));
        }

        public Task Set<T>(string key, T value)
        {
            return _table.InsertOrReplaceAsync(new SettingsEntity(key, value?.ToString()));
        }
    }
}
