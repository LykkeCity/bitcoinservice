using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Features.ResolveAnything;
using AzureRepositories.Offchain;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Lykke.SettingsReader;
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
                    AzureTableStorage<OffchainRequestEntity>.Create(new FakeReloadingManager(configuration.GetConnectionString("main")), "OffchainRequests", null)));

            container.RegisterInstance<IOffchainTransferRepository>(
                new OffchainTransferRepository(
                    AzureTableStorage<OffchainTransferEntity>.Create(new FakeReloadingManager(configuration.GetConnectionString("main")), "OffchainTransfers", null)));

            container.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());

            return container.Build();
        }
    }

    public class FakeReloadingManager : IReloadingManager<string>
    {
        private readonly string _value;

        public FakeReloadingManager(string value)
        {
            _value = value;
        }

        public Task<string> Reload() => Task.FromResult(_value);
        public bool HasLoaded => true;
        public string CurrentValue => _value;
    }
}
