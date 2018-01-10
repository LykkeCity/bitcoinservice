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
using AzureStorage.Tables;
using Common;
using Common.Cache;
using Common.IocContainer;
using Common.Log;
using Core.Bitcoin;
using Core.Settings;
using Microsoft.Extensions.Configuration;
using LkeServices;
using Lykke.JobTriggers.Abstractions.QueueReader;
using Lykke.JobTriggers.Implementations.QueueReader;
using Microsoft.Extensions.DependencyInjection;
using Lykke.JobTriggers.Extenstions;
using Autofac.Extensions.DependencyInjection;
using MongoRepositories;

namespace BackgroundWorker.Binders
{
    public class AzureBinder
    {
        public const string DefaultConnectionString = "UseDevelopmentStorage=true";

        public ContainerBuilder Bind(BaseSettings settings, SlackNotifications slackNotifications)
        {
            var logToTable = new LogToTable(new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LogBitcoinJobError", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LogBitcoinJobWarning", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LogBitcoinJobInfo", null));
#if DEBUG
            var log = new LogToTableAndConsole(logToTable, new LogToConsole());
#else
            var log = logToTable;
#endif
            var ioc = new ContainerBuilder();
            InitContainer(ioc, settings, slackNotifications, log);
            return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, BaseSettings settings, SlackNotifications slackNotifications, ILog log)
        {
#if DEBUG
            log.WriteInfoAsync("BackgroundWorker", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();
#else
            log.WriteInfoAsync("BackgroundWorker", "App start", null, $"BaseSettings : private").Wait();
#endif            
            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings);
            ioc.RegisterInstance(new RpcConnectionParams(settings));

            ioc.BindCommonServices();
            ioc.BindAzure(settings, slackNotifications, log);
            ioc.BindMongo(settings);

            ioc.AddTriggers(pool =>
            {
                pool.AddDefaultConnection(settings.Db.DataConnString);
                pool.AddConnection("client", settings.Db.ClientSignatureConnString);
            });

            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }
    }
}
