using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using AzureRepositories.Assets;
using AzureRepositories.FeeRate;
using AzureRepositories.Monitoring;
using AzureRepositories.Offchain;
using AzureRepositories.TransactionOutputs;
using AzureRepositories.Transactions;
using AzureRepositories.TransactionSign;
using AzureRepositories.Walelts;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Common.Log;
using Core;
using Core.Repositories;
using Core.Repositories.Assets;
using Core.Repositories.FeeRate;
using Core.Repositories.Monitoring;
using Core.Repositories.Offchain;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Core.Repositories.Wallets;
using Core.Settings;

namespace AzureRepositories
{
    public static class RepoBinder
    {
        public static void BindAzure(this ContainerBuilder ioc, BaseSettings settings)
        {
            ioc.BindRepo(settings);
            ioc.BindQueue(settings);
        }

        private static void BindRepo(this ContainerBuilder ioc, BaseSettings settings)
        {
            ioc.RegisterInstance(new FeeRateRepository(new AzureTableStorage<FeeRateEntity>(settings.Db.DataConnString, "Settings", null)))
                .As<IFeeRateRepository>();

            ioc.RegisterInstance(new SpentOutputRepository(new AzureTableStorage<OutputEntity>(settings.Db.DataConnString, "SpentOutputs", null)))
                .As<ISpentOutputRepository>();

            ioc.RegisterInstance(new BroadcastedTransactionRepository(new AzureTableStorage<BroadcastedTransactionEntity>(settings.Db.DataConnString, "BroadcastedTransactions", null)))
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
        }

        private static void BindQueue(this ContainerBuilder ioc, BaseSettings settings)
        {
            ioc.RegisterInstance(new SignedTransactionQueue(new AzureQueueExt(settings.Db.InQueueConnString, Constants.SignedTransactionsQueue)))
                .As<ISignedTransactionQueue>();

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
        }
    }
}
