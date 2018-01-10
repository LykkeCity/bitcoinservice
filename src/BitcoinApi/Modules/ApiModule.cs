using Autofac;
using AzureRepositories;
using Common.Log;
using Core.Settings;
using LkeServices;
using Lykke.SettingsReader;
using MongoRepositories;

namespace BitcoinApi.Modules
{
    public class ApiModule : Module
    {
        private readonly BaseSettings _settings;
        private readonly IReloadingManager<DbSettings> _dbSettingsManager;
        private readonly ILog _log;

        public ApiModule(BaseSettings settings, IReloadingManager<DbSettings> db, ILog log)
        {
            _settings = settings;
            _dbSettingsManager = db;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log);
            builder.RegisterInstance(_settings);
            builder.RegisterInstance(new RpcConnectionParams(_settings));

            builder.BindCommonServices();
            builder.BindAzure(_dbSettingsManager, _log);
            builder.BindMongo(_dbSettingsManager.ConnectionString(x => x.MongoDataConnString).CurrentValue);
        }
    }
}
