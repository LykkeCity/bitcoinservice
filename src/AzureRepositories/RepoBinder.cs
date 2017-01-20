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
        public static void BindAzure(this ContainerBuilder ioc, BaseSettings settings)
        {
            ioc.BindRepo(settings);
            ioc.BindQueue(settings);

            ioc.RegisterType<EmailNotifier>().As<IEmailNotifier>();
            ioc.RegisterType<SlackNotifier>().As<ISlackNotifier>();
        }

        private static void BindRepo(this ContainerBuilder ioc, BaseSettings settings)
        {
            ioc.RegisterInstance(new FeeRateRepository(new AzureTableStorage<FeeRateEntity>(settings.Db.DataConnString, "Settings", null)))
                .As<IFeeRateRepository>();

            ioc.RegisterInstance(new SpentOutputRepository(new AzureTableStorage<OutputEntity>(settings.Db.DataConnString, "SpentOutputs", null)))
                .As<ISpentOutputRepository>();

            ioc.RegisterInstance(new BroadcastedTransactionRepository(
                new AzureTableStorage<BroadcastedTransactionEntity>(settings.Db.DataConnString, "BroadcastedTransactions", null),
                new AzureBlobStorage(settings.Db.DataConnString)))
                .As<IBroadcastedTransactionRepository>();

            ioc.RegisterInstance(new AssetRepository(new AzureTableStorage<AssetEntity>(settings.Db.DictsConnString, "Dictionaries", null)))
                .As<IAssetRepository>();

            ioc.RegisterInstance(new TransactionSignRequestRepository(new AzureTableStorage<TransactionSignRequestEntity>(settings.Db.DataConnString, "SignRequests", null)))
                .As<ITransactionSignRequestRepository>();

            ioc.RegisterInstance(new WalletAddressRepository(new AzureTableStorage<WalletAddressEntity>(settings.Db.DataConnString, "Wallets", null)))
                .As<IWalletAddressRepository>();

            ioc.RegisterInstance(new BroadcastedOutputRepository(new AzureTableStorage<BroadcastedOutputEntity>(settings.Db.DataConnString, "BroadcastedOutputs", null)))
                .As<IBroadcastedOutputRepository>();

            ioc.RegisterInstance(new CommitmentRepository(new AzureTableStorage<CommitmentEntity>(settings.Db.DataConnString, "Commitments", null)))
                .As<ICommitmentRepository>();

            ioc.RegisterInstance(new OffchainChannelRepository(new AzureTableStorage<OffchainChannelEntity>(settings.Db.DataConnString, "Channels", null)))
                .As<IOffchainChannelRepository>();

            ioc.RegisterInstance(new MonitoringRepository(new AzureTableStorage<MonitoringEntity>(settings.Db.SharedConnString, "Monitoring", null)))
                .As<IMonitoringRepository>();

            ioc.RegisterInstance(new FailedTransactionRepository(new AzureTableStorage<FailedTransactionEntity>(settings.Db.ClientPersonalInfoConnString, "FailedTransactions", null)))
                .As<IFailedTransactionRepository>();

            ioc.RegisterInstance(new ApiRequestBlobRepository(new AzureBlobStorage(settings.Db.DataConnString)))
                .As<IApiRequestBlobRepository>();
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
        }
    }
}
