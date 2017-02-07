using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using AzureRepositories.ApiRequests;
using AzureRepositories.Assets;
using AzureRepositories.FeeRate;
using AzureRepositories.Monitoring;
using AzureRepositories.Notifiers;
using AzureRepositories.Offchain;
using AzureRepositories.Settings;
using AzureRepositories.TransactionMonitoring;
using AzureRepositories.TransactionOutputs;
using AzureRepositories.TransactionQueueHolder;
using AzureRepositories.Transactions;
using AzureRepositories.TransactionSign;
using AzureRepositories.Walelts;
using AzureStorage.Blob;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Common.Log;
using Core;
using Core.Notifiers;
using Core.Repositories;
using Core.Repositories.ApiRequests;
using Core.Repositories.Assets;
using Core.Repositories.FeeRate;
using Core.Repositories.Monitoring;
using Core.Repositories.Offchain;
using Core.Repositories.Settings;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Core.Repositories.Wallets;
using Core.Settings;
using Core.TransactionMonitoring;
using Core.TransactionQueueWriter;

namespace AzureRepositories
{
    public static class RepoBinder
    {
        public static void BindAzure(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            ioc.BindRepo(settings, log);
            ioc.BindQueue(settings);

            ioc.RegisterType<EmailNotifier>().As<IEmailNotifier>();
            ioc.RegisterType<SlackNotifier>().As<ISlackNotifier>();
        }

        private static void BindRepo(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            ioc.RegisterInstance(new FeeRateRepository(new AzureTableStorage<FeeRateEntity>(settings.Db.DataConnString, "Settings", log)))
                .As<IFeeRateRepository>();

            ioc.RegisterInstance(new SettingsRepository(new AzureTableStorage<SettingsEntity>(settings.Db.DataConnString, "Settings", log)))
                .As<ISettingsRepository>();

            ioc.RegisterInstance(new SpentOutputRepository(new AzureTableStorage<OutputEntity>(settings.Db.DataConnString, "SpentOutputs", log)))
                .As<ISpentOutputRepository>();

            ioc.RegisterInstance(new BroadcastedTransactionRepository(
                new AzureTableStorage<BroadcastedTransactionEntity>(settings.Db.DataConnString, "BroadcastedTransactions", log),
                new AzureBlobStorage(settings.Db.DataConnString)))
                .As<IBroadcastedTransactionRepository>();

            ioc.RegisterInstance(new AssetRepository(new AzureTableStorage<AssetEntity>(settings.Db.DictsConnString, "Dictionaries", log)))
                .As<IAssetRepository>();

            ioc.RegisterInstance(new TransactionSignRequestRepository(new AzureTableStorage<TransactionSignRequestEntity>(settings.Db.DataConnString, "SignRequests", log)))
                .As<ITransactionSignRequestRepository>();

            ioc.RegisterInstance(new WalletAddressRepository(new AzureTableStorage<WalletAddressEntity>(settings.Db.DataConnString, "Wallets", log)))
                .As<IWalletAddressRepository>();

            ioc.RegisterInstance(new BroadcastedOutputRepository(new AzureTableStorage<BroadcastedOutputEntity>(settings.Db.DataConnString, "BroadcastedOutputs", log)))
                .As<IBroadcastedOutputRepository>();

            ioc.RegisterInstance(new CommitmentRepository(new AzureTableStorage<CommitmentEntity>(settings.Db.DataConnString, "Commitments", log)))
                .As<ICommitmentRepository>();

            ioc.RegisterInstance(new OffchainChannelRepository(new AzureTableStorage<OffchainChannelEntity>(settings.Db.DataConnString, "Channels", log)))
                .As<IOffchainChannelRepository>();

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
                    default:
                        return new AzureQueueExt(settings.Db.DataConnString, queueName);
                }

            });
            ioc.RegisterType<PregeneratedOutputsQueueFactory>().As<IPregeneratedOutputsQueueFactory>().SingleInstance();

            ioc.RegisterType<TransactionQueueWriter>().As<ITransactionQueueWriter>().SingleInstance();
            ioc.RegisterType<TransactionMonitoringWriter>().As<ITransactionMonitoringWriter>().SingleInstance();
            ioc.RegisterType<FeeReserveMonitoringWriter>().As<IFeeReserveMonitoringWriter>().SingleInstance();
        }
    }
}
