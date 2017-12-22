using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;

using Common;
using Common.Log;
using Core;
using Core.Notifiers;
using Core.Repositories;
using Core.Repositories.ApiRequests;
using Core.Repositories.Assets;
using Core.Repositories.ExtraAmounts;
using Core.Repositories.FeeRate;
using Core.Repositories.Monitoring;
using Core.Repositories.Offchain;
using Core.Repositories.PaidFees;
using Core.Repositories.RevokeKeys;
using Core.Repositories.Settings;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Core.Repositories.Wallets;
using Core.Settings;
using Core.TransactionMonitoring;
using Core.TransactionQueueWriter;
using MongoDB.Driver;
using MongoRepositories.ExtraAmounts;
using MongoRepositories.FeeRate;
using MongoRepositories.Mongo;
using MongoRepositories.Offchain;
using MongoRepositories.PaidFees;
using MongoRepositories.RevokeKeys;
using MongoRepositories.Settings;
using MongoRepositories.TransactionOutputs;
using MongoRepositories.Transactions;
using MongoRepositories.TransactionSign;
using MongoRepositories.Walelts;

namespace MongoRepositories
{
    public static class RepoBinder
    {      
        public static void BindMongo(this ContainerBuilder ioc, BaseSettings settings)
        {

            var mongoClient = new MongoClient(settings.Db.MongoDataConnString);

            ioc.RegisterInstance(new FeeRateRepository(new MongoStorage<FeeRateEntity>(mongoClient ,"Settings")))
                .As<IFeeRateRepository>();

            ioc.RegisterInstance(new SettingsRepository(new MongoStorage<SettingsEntity>(mongoClient, "Settings")))
                .As<ISettingsRepository>();

            ioc.RegisterInstance(new SpentOutputRepository(new MongoStorage<OutputEntity>(mongoClient, "SpentOutputs")))
                .As<ISpentOutputRepository>();           

            ioc.RegisterInstance(new BroadcastedTransactionRepository(new MongoStorage<BroadcastedTransactionEntity>(mongoClient, "BroadcastedTransactions")))
                .As<IBroadcastedTransactionRepository>();
        
            ioc.RegisterInstance(new TransactionSignRequestRepository(new MongoStorage<TransactionSignRequestEntity>(mongoClient, "SignRequests")))
                .As<ITransactionSignRequestRepository>();

            ioc.RegisterInstance(new WalletAddressRepository(new MongoStorage<WalletAddressEntity>(mongoClient, "Wallets")))
                .As<IWalletAddressRepository>();

            ioc.RegisterInstance(new BroadcastedOutputRepository(new MongoStorage<BroadcastedOutputEntity>(mongoClient, "BroadcastedOutputs")))
                .As<IBroadcastedOutputRepository>();

            ioc.RegisterInstance(new CommitmentRepository(new MongoStorage<CommitmentEntity>(mongoClient, "Commitments")))
                .As<ICommitmentRepository>();

            ioc.RegisterInstance(new OffchainChannelRepository(new MongoStorage<OffchainChannelEntity>(mongoClient, "Channels")))
                .As<IOffchainChannelRepository>();
           
            ioc.RegisterInstance(new RevokeKeyRepository(new MongoStorage<RevokeKeyEntity>(mongoClient, "RevokeKeys")))
                .As<IRevokeKeyRepository>();

            ioc.RegisterInstance(new ExtraAmountRepository(new MongoStorage<ExtraAmountEntity>(mongoClient, "ExtraAmounts")))
                .As<IExtraAmountRepository>();

            ioc.RegisterInstance(new OffchainTransferRepository(new MongoStorage<OffchainTransferEntity>(mongoClient, "Transfers")))
                .As<IOffchainTransferRepository>();

            ioc.RegisterInstance(new ClosingChannelRepository(new MongoStorage<ClosingChannelEntity>(mongoClient, "ClosingChannel")))
                .As<IClosingChannelRepository>();

            ioc.RegisterInstance(new InternalSpentOutputRepository(new MongoStorage<InternalSpentOutput>(mongoClient, "InternalSpentOutputs")))
                .As<IInternalSpentOutputRepository>();

            ioc.RegisterInstance(new CommitmentBroadcastRepository(new MongoStorage<CommitmentBroadcastEntity>(mongoClient, "CommitmentBroadcasts")))
                .As<ICommitmentBroadcastRepository>();

            ioc.RegisterInstance(new SpentOutputRepository(new MongoStorage<OutputEntity>(mongoClient, "BccSpentOutputs")))
                .Keyed<ISpentOutputRepository>(Constants.BccKey);

            ioc.RegisterInstance(new PaidFeesRepository(new MongoStorage<PaidFeesEntity>(mongoClient, "PaidFees")))
                .As<IPaidFeesRepository>();

            ioc.RegisterInstance(new SegwitPrivateWalletRepository(new MongoStorage<SegwitPrivateWalletEntity>(mongoClient, "SegwitWallets")))
                .As<ISegwitPrivateWalletRepository>();
        }        
    }
}
