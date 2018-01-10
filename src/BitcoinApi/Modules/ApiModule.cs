using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Autofac.Features.ResolveAnything;
using AzureRepositories;
using BitcoinApi.Services;
using Common.Log;
using Core.Settings;
using LkeServices;
using Lykke.JobTriggers.Extenstions;
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
            builder.RegisterType<RetryFailedTransactionService>().As<IRetryFailedTransactionService>();

            builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }
    }
}
