using Autofac;
using Autofac.Features.ResolveAnything;
using AzureRepositories;
using AzureRepositories.Log;
using AzureRepositories.QueueReader;
using AzureStorage.Tables;
using Common;
using Common.IocContainer;
using Common.Log;
using Core.Bitcoin;
using Core.QueueReader;
using Core.Settings;
using LkeServices;
using Microsoft.Extensions.Configuration;

namespace BitcoinApi.Binders
{
    public class AzureBinder
    {
        public const string DefaultConnectionString = "UseDevelopmentStorage=true";

        public ContainerBuilder Bind(BaseSettings settings)
        {
            var logToTable = new LogToTable(new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "BitcoinApiError", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "BitcoinApiWarning", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "BitcoinApiInfo", null));
            var log = new LogToTableAndConsole(logToTable, new LogToConsole());
            var ioc = new ContainerBuilder();
            InitContainer(ioc, settings, log);
            return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            log.WriteInfoAsync("BitcoinApi", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings);
            ioc.RegisterInstance(new RpcConnectionParams(settings));

            ioc.BindCommonServices();
            ioc.BindAzure(settings);
            
            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }        
    }
}
