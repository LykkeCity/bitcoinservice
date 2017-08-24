

using Autofac;
using Autofac.Features.ResolveAnything;
using Core.Repositories.Offchain;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoRepositories.Mongo;
using MongoRepositories.Offchain;

namespace ExportBccBalances
{
    public class DependencyBinder
    {
        public static IContainer BindAndBuild(IConfigurationRoot configuration)
        {
            var container = new ContainerBuilder();

            var mongoClient = new MongoClient(configuration["Mongo"]);

            container.RegisterInstance(new CommitmentRepository(new MongoStorage<CommitmentEntity>(mongoClient, "Commitments")))
                .As<ICommitmentRepository>();

            container.RegisterInstance(new OffchainChannelRepository(new MongoStorage<OffchainChannelEntity>(mongoClient, "Channels")))
                .As<IOffchainChannelRepository>();

            container.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());

            return container.Build();
        }
    }
}
