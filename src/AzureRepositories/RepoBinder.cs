using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using AzureRepositories.ApiRequests;
using AzureRepositories.Assets;
using AzureRepositories.ExtraAmounts;
using AzureRepositories.FeeRate;
using AzureRepositories.Monitoring;
using AzureRepositories.Notifiers;
using AzureRepositories.Offchain;
using AzureRepositories.RevokeKeys;
using AzureRepositories.Settings;
using AzureRepositories.TransactionMonitoring;
using AzureRepositories.TransactionOutputs;
using AzureRepositories.TransactionQueueHolder;
using AzureRepositories.Transactions;
using AzureRepositories.TransactionSign;
using AzureRepositories.Walelts;
using AzureStorage;
using AzureStorage.Blob;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Core;
using Core.Notifiers;
using Core.Outputs;
using Core.Repositories;
using Core.Repositories.ApiRequests;
using Core.Repositories.Assets;
using Core.Repositories.ExtraAmounts;
using Core.Repositories.FeeRate;
using Core.Repositories.Monitoring;
using Core.Repositories.Offchain;
using Core.Repositories.RevokeKeys;
using Core.Repositories.Settings;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Core.Repositories.Wallets;
using Core.Settings;
using Core.TransactionMonitoring;
using Core.TransactionQueueWriter;
using Lykke.JobTriggers.Abstractions;

namespace AzureRepositories
{
    public static class RepoBinder
    {
        public static void BindAzure(this ContainerBuilder ioc, BaseSettings settings, ILog log)
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

        private static void BindRepo(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
         
            ioc.RegisterInstance(new BroadcastedTransactionBlobStorage(         
                new AzureBlobStorage(settings.Db.DataConnString)))
                .As<IBroadcastedTransactionBlobStorage>();

            ioc.RegisterInstance(new AssetRepository(new AzureTableStorage<AssetEntity>(settings.Db.DictsConnString, "Dictionaries", log)))
                .As<IAssetRepository>();

            ioc.RegisterInstance(new AssetSettingRepository(new AzureTableStorage<AssetSettingEntity>(settings.Db.DictsConnString, "AssetSettings", log)))
                .As<IAssetSettingRepository>();

            ioc.RegisterInstance(new MonitoringRepository(new AzureTableStorage<MonitoringEntity>(settings.Db.SharedConnString, "Monitoring", log)))
                .As<IMonitoringRepository>();

            ioc.RegisterInstance(new FailedTransactionRepository(new AzureTableStorage<FailedTransactionEntity>(settings.Db.ClientPersonalInfoConnString, "FailedTransactions", log)))
                .As<IFailedTransactionRepository>();

            ioc.RegisterInstance(new MenuBadgesRepository(new AzureTableStorage<MenuBadgeEntity>(settings.Db.BackofficeConnString, "MenuBadges", log)))
                .As<IMenuBadgesRepository>();

            ioc.RegisterInstance(new ApiRequestBlobRepository(new AzureBlobStorage(settings.Db.LogsConnString)))
                .As<IApiRequestBlobRepository>();

            ioc.RegisterInstance(new TransactionBlobStorage(new AzureBlobStorage(settings.Db.DataConnString)))
                .As<ITransactionBlobStorage>();
    
            ioc.RegisterInstance(new NinjaOutputBlobStorage(new AzureBlobStorage(settings.Db.DataConnString)))
                .As<INinjaOutputBlobStorage>();

        }

        private static void BindQueue(this ContainerBuilder ioc, BaseSettings settings)
        {

            ioc.RegisterInstance<Func<string, IQueueExt>>(queueName =>
            {
                switch (queueName)
                {
                    case Constants.SlackNotifierQueue:
                    case Constants.EmailNotifierQueue:
                        return new AzureQueueExt(settings.Db.SharedConnString, queueName);
                    case Constants.TransactionsForClientSignatureQueue:
                        return new AzureQueueExt(settings.Db.ClientSignatureConnString, queueName);
                    default:
                        return new AzureQueueExt(settings.Db.DataConnString, queueName);
                }

            });
            ioc.RegisterType<PregeneratedOutputsQueueFactory>().As<IPregeneratedOutputsQueueFactory>().SingleInstance();

            ioc.RegisterType<TransactionQueueWriter>().As<ITransactionQueueWriter>().SingleInstance();
            ioc.RegisterType<TransactionMonitoringWriter>().As<ITransactionMonitoringWriter>().SingleInstance();
            ioc.RegisterType<FeeReserveMonitoringWriter>().As<IFeeReserveMonitoringWriter>().SingleInstance();
            ioc.RegisterType<ReturnBroadcastedOutputsMessageWriter>().As<IReturnBroadcastedOutputsMessageWriter>().SingleInstance();
            ioc.RegisterType<SpendCommitmentMonitoringWriter>().As<ISpendCommitmentMonitoringWriter>().SingleInstance();
        }
    }
}
