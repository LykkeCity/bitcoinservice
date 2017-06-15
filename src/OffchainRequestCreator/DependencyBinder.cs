using System;
using Autofac;
using Autofac.Features.ResolveAnything;
using AzureRepositories.Offchain;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OffchainRequestCreator.Repositories;
using PoisonMessagesReenqueue;
using OffchainTransferEntity = OffchainRequestCreator.Repositories.OffchainTransferEntity;
using OffchainTransferRepository = OffchainRequestCreator.Repositories.OffchainTransferRepository;

namespace OffchainRequestCreator
{
    public class DependencyBinder
    {
        public static IContainer BindAndBuild(IConfigurationRoot configuration)
        {
            var container = new ContainerBuilder();

            container.RegisterInstance<IOffchainRequestRepository>(
                new OffchainRequestRepository(
                    new AzureTableStorage<OffchainRequestEntity>(configuration.GetConnectionString("main"), "OffchainRequests", null)));

            container.RegisterInstance<IOffchainTransferRepository>(
                new OffchainTransferRepository(
                    new AzureTableStorage<OffchainTransferEntity>(configuration.GetConnectionString("main"), "OffchainTransfers", null)));

            container.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());

            return container.Build();
        }
    }
}
