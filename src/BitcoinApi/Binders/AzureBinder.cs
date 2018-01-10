using Autofac;
using Autofac.Features.ResolveAnything;
using AzureRepositories;
using AzureRepositories.Log;
using AzureStorage.Tables;
using BitcoinApi.Services;
using Common;
using Common.IocContainer;
using Common.Log;
using Core.Bitcoin;
using Core.Settings;
using LkeServices;
using Microsoft.Extensions.Configuration;
using MongoRepositories;

namespace BitcoinApi.Binders
{
    public class AzureBinder
    {
        public const string DefaultConnectionString = "UseDevelopmentStorage=true";

        public ContainerBuilder Bind(BaseSettings settings, SlackNotifications slackNotifications)
        {
            var logToTable = new LogToTable(new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "BitcoinApiError", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "BitcoinApiWarning", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "BitcoinApiInfo", null));
            var log = new LogToTableAndConsole(logToTable, new LogToConsole());
            var ioc = new ContainerBuilder();
            InitContainer(ioc, settings, slackNotifications, log);
            return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, BaseSettings settings, SlackNotifications slackNotifications, ILog log)
        {
#if DEBUG
            log.WriteInfoAsync("BitcoinApi", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();
#else
            log.WriteInfoAsync("BitcoinApi", "App start", null, $"BaseSettings : private").Wait();
#endif

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings);            
            ioc.RegisterInstance(new RpcConnectionParams(settings));

            ioc.BindCommonServices();
            ioc.BindAzure(settings, slackNotifications, log);
            ioc.BindMongo(settings);
            ioc.RegisterType<RetryFailedTransactionService>().As<IRetryFailedTransactionService>();

            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }        
    }
}
