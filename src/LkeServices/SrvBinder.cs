using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Core.Bitcoin;
using Core.Providers;
using Core.QBitNinja;
using Core.Repositories.TransactionSign;
using Core.Settings;
using LkeServices.Bitcoin;
using LkeServices.Multisig;
using LkeServices.Providers;
using LkeServices.Providers.Rest;
using LkeServices.QBitNinja;
using LkeServices.Signature;
using LkeServices.Transactions;
using PhoneNumbers;
using QBitNinja.Client;
using RestClient = RestEase.RestClient;

namespace LkeServices
{
    public static class SrvBinder
    {
        public static void BindCommonServices(this ContainerBuilder ioc)
        {
            ioc.Register<Func<QBitNinjaClient>>(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                return () =>
                {
                    var settings = resolver.Resolve<BaseSettings>();
                    var connectionParams = resolver.Resolve<RpcConnectionParams>();
                    return new QBitNinjaClient(settings.QBitNinjaBaseUrl, connectionParams.Network);
                };
            });

            ioc.RegisterType<QBitNinjaApiCaller>().As<IQBitNinjaApiCaller>();

            ioc.RegisterType<RpcBitcoinClient>().As<IRpcBitcoinClient>();

            ioc.RegisterType<BitcoinOutputsService>().As<IBitcoinOutputsService>();
            ioc.RegisterType<FeeProvider>().As<IFeeProvider>();

            ioc.RegisterType<LykkeTransactionBuilderService>().As<ILykkeTransactionBuilderService>();
            ioc.RegisterType<OffchainTransactionBuilderService>().As<IOffchainTransactionBuilderService>();
            ioc.RegisterType<MultisigService>().As<IMultisigService>();
            ioc.RegisterType<TransactionBuildHelper>().As<ITransactionBuildHelper>();
            ioc.RegisterType<BitcoinTransactionService>().As<IBitcoinTransactionService>();
            ioc.RegisterType<OffchainTransactionBuilderService>().As<IOffchainTransactionBuilderService>();
            ioc.RegisterType<SignatureVerifier>().As<ISignatureVerifier>();
            ioc.RegisterType<BitcoinBroadcastService>().As<IBitcoinBroadcastService>();
            ioc.RegisterType<FailedTransactionsManager>().As<IFailedTransactionsManager>();


            BindApiProviders(ioc);
        }

        private static void BindApiProviders(ContainerBuilder ioc)
        {
            ioc.RegisterType<LykkeHttpClientHandler>();
            ioc.Register(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var lykkyHttpClientHandler = resolver.Resolve<LykkeHttpClientHandler>();
                var client = new HttpClient(lykkyHttpClientHandler)
                {
                    Timeout = TimeSpan.FromMinutes(5),
                    BaseAddress = new Uri("https://bitcoinfees.21.co/api/v1")
                };
                return RestClient.For<IFeeRateApiProvider>(client);
            }).As<IFeeRateApiProvider>().SingleInstance();

            ioc.Register(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var lykkyHttpClientHandler = resolver.Resolve<LykkeHttpClientHandler>();
                var settings = resolver.Resolve<BaseSettings>();
                var client = new HttpClient(lykkyHttpClientHandler)
                {
                    BaseAddress = new Uri(settings.LykkeJobsUrl)
                };
                return RestClient.For<ILykkeApiProvider>(client);
            }).As<ILykkeApiProvider>().SingleInstance();


            ioc.Register<Func<SignatureApiProviderType, ISignatureApi>>(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var settings = resolver.Resolve<BaseSettings>();

                return type =>
                {
                    var lykkyHttpClientHandler = resolver.Resolve<LykkeHttpClientHandler>();
                    var client = new HttpClient(lykkyHttpClientHandler)
                    {
                        Timeout = TimeSpan.FromMinutes(5),
                        BaseAddress = new Uri(type == SignatureApiProviderType.Client
                            ? settings.ClientSignatureProviderUrl
                            : settings.SignatureProviderUrl)
                    };
                    return RestClient.For<ISignatureApi>(client);
                };
            }).SingleInstance();

            ioc.Register<Func<SignatureApiProviderType, ISignatureApiProvider>>(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var factory = resolver.Resolve<Func<SignatureApiProviderType, ISignatureApi>>();
                return type => new SignatureApiProvider(factory(type));
            }).SingleInstance();
        }
    }
}
