using System;
using System.Linq;
using Autofac;
using AzureRepositories.ApiRequests;
using AzureRepositories.Assets;
using AzureRepositories.Monitoring;
using AzureRepositories.Notifiers;
using AzureRepositories.Offchain;
using AzureRepositories.PaidFees;
using AzureRepositories.TransactionMonitoring;
using AzureRepositories.TransactionOutputs;
using AzureRepositories.TransactionQueueHolder;
using AzureRepositories.Transactions;
using AzureStorage.Blob;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Core;
using Core.Notifiers;
using Core.Outputs;
using Core.Repositories.ApiRequests;
using Core.Repositories.Assets;
using Core.Repositories.Monitoring;
using Core.Repositories.Offchain;
using Core.Repositories.PaidFees;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using Core.Settings;
using Core.TransactionMonitoring;
using Core.TransactionQueueWriter;
using Lykke.JobTriggers.Abstractions;
using Lykke.SettingsReader;

namespace AzureRepositories
{
    public static class RepoBinder
    {
        public static void BindAzure(this ContainerBuilder ioc, IReloadingManager<DbSettings> settings, ILog log)
        {
            ioc.BindRepo(settings, log);
            ioc.BindQueue(settings);

            ioc.Register(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return new CachedDataDictionary<string, IAsset>(async () => (await ctx.Resolve<IAssetRepository>().GetBitcoinAssets()).ToDictionary(itm => itm.Id));
            }).SingleInstance();

            ioc.Register(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return new CachedDataDictionary<string, IAssetSetting>(
                    async () => (await ctx.Resolve<IAssetSettingRepository>().GetAssetSettings()).ToDictionary(itm => itm.Asset));
            }).SingleInstance();

            ioc.RegisterType<EmailNotifier>().As<IEmailNotifier>();
            ioc.RegisterType<SlackNotifier>().As<ISlackNotifier>().As<IPoisionQueueNotifier>();
        }

        private static void BindRepo(this ContainerBuilder ioc, IReloadingManager<DbSettings> settings, ILog log)
        {

            ioc.RegisterInstance(new BroadcastedTransactionBlobStorage(
                AzureBlobStorage.Create(settings.ConnectionString(x => x.DataConnString))))
                .As<IBroadcastedTransactionBlobStorage>();

            ioc.RegisterInstance(new AssetRepository(AzureTableStorage<AssetEntity>.Create(settings.ConnectionString(x => x.DictsConnString), "Dictionaries", log)))
                .As<IAssetRepository>();

            ioc.RegisterInstance(new AssetSettingRepository(AzureTableStorage<AssetSettingEntity>.Create(settings.ConnectionString(x => x.DictsConnString), "AssetSettings", log)))
                .As<IAssetSettingRepository>();

            ioc.RegisterInstance(new MonitoringRepository(AzureTableStorage<MonitoringEntity>.Create(settings.ConnectionString(x => x.SharedConnString), "Monitoring", log)))
                .As<IMonitoringRepository>();

            ioc.RegisterInstance(new ApiRequestBlobRepository(AzureBlobStorage.Create(settings.ConnectionString(x => x.LogsConnString))))
                .As<IApiRequestBlobRepository>();

            ioc.RegisterInstance(new TransactionBlobStorage(AzureBlobStorage.Create(settings.ConnectionString(x => x.DataConnString))))
                .As<ITransactionBlobStorage>();

            ioc.RegisterInstance(new NinjaOutputBlobStorage(AzureBlobStorage.Create(settings.ConnectionString(x => x.DataConnString))))
                .As<INinjaOutputBlobStorage>();

        }

        private static void BindQueue(this ContainerBuilder ioc, IReloadingManager<DbSettings> settings)
        {

            ioc.RegisterInstance<Func<string, IQueueExt>>(queueName =>
            {
                switch (queueName)
                {
                    case Constants.EmailNotifierQueue:
                        return AzureQueueExt.Create(settings.ConnectionString(x => x.SharedConnString), queueName);
                    default:
                        return AzureQueueExt.Create(settings.ConnectionString(x => x.DataConnString), queueName);
                }

            });
            ioc.RegisterType<PregeneratedOutputsQueueFactory>().As<IPregeneratedOutputsQueueFactory>().SingleInstance();

            ioc.RegisterType<TransactionQueueWriter>().As<ITransactionQueueWriter>().SingleInstance();
            ioc.RegisterType<TransactionMonitoringWriter>().As<ITransactionMonitoringWriter>().SingleInstance();
            ioc.RegisterType<FeeReserveMonitoringWriter>().As<IFeeReserveMonitoringWriter>().SingleInstance();
            ioc.RegisterType<ReturnOutputsMessageWriter>().As<IReturnOutputsMessageWriter>().SingleInstance();
            ioc.RegisterType<SpendCommitmentMonitoringWriter>().As<ISpendCommitmentMonitoringWriter>().SingleInstance();
            ioc.RegisterType<PaidFeesTaskWriter>().As<IPaidFeesTaskWriter>().SingleInstance();
            ioc.RegisterType<CommitmentClosingTaskWriter>().As<ICommitmentClosingTaskWriter>().SingleInstance();
        }
    }
}
