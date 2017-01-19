using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Features.ResolveAnything;
using AzureRepositories;
using AzureRepositories.Log;
using AzureRepositories.Notifiers;
using AzureRepositories.QueueReader;
using AzureStorage.Tables;
using Common;
using Common.Cache;
using Common.IocContainer;
using Common.Log;
using Core.Bitcoin;
using Core.Settings;
using Microsoft.Extensions.Configuration;

using Core.QueueReader;
using LkeServices;

namespace BackgroundWorker.Binders
{
    public class AzureBinder
    {
        public const string DefaultConnectionString = "UseDevelopmentStorage=true";

        public ContainerBuilder Bind(BaseSettings settings)
        {
            var logToTable = new LogToTable(new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LogBitcoinJobError", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LogBitcoinJobWarning", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LogBitcoinJobInfo", null));
            var log = new LogToTableAndConsole(logToTable, new LogToConsole());
            var ioc = new ContainerBuilder();
            InitContainer(ioc, settings, log);
            return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            log.WriteInfoAsync("BackgroundWorker", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings);
            ioc.RegisterInstance(new RpcConnectionParams(settings));

            ioc.BindCommonServices();
            ioc.BindAzure(settings);

            ioc.RegisterInstance(new AzureQueueReaderFactory(settings.Db.DataConnString)).As<IQueueReaderFactory>();

            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }
    }
}
