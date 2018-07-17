using System;
using System.Net.Http;
using Autofac;
using Autofac.Features.AttributeFilters;
using Core;
using Core.Bcc;
using Core.Bitcoin;
using Core.OpenAssets;
using Core.Outputs;
using Core.Performance;
using Core.Providers;
using Core.QBitNinja;
using LkeServices.Bcc;
using LkeServices.Bitcoin;
using LkeServices.Outputs;
using LkeServices.Performance;
using LkeServices.Providers;
using LkeServices.QBitNinja;
using LkeServices.Signature;
using LkeServices.Transactions;
using LkeServices.Wallet;
using QBitNinja.Client;
using BaseSettings = Core.Settings.BaseSettings;
using RestClient = RestEase.RestClient;
using RpcConnectionParams = Core.Settings.RpcConnectionParams;

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
            ioc.RegisterType<OffchainService>().As<IOffchainService>();
            ioc.RegisterType<WalletService>().As<IWalletService>();
            ioc.RegisterType<TransactionBuildHelper>().As<ITransactionBuildHelper>();
            ioc.RegisterType<BitcoinTransactionService>().As<IBitcoinTransactionService>();
            ioc.RegisterType<OffchainService>().As<IOffchainService>();
            ioc.RegisterType<SignatureVerifier>().As<ISignatureVerifier>();
            ioc.RegisterType<BitcoinBroadcastService>().As<IBitcoinBroadcastService>();
            ioc.RegisterType<PerformanceMonitorFactory>().As<IPerformanceMonitorFactory>();
            ioc.RegisterType<SpentOutputService>().As<ISpentOutputService>();

            ioc.RegisterType<TransactionBuildContextFactory>();
            ioc.RegisterType<TransactionBuildContext>();

            BindApiProviders(ioc);

            BindBccServices(ioc);
        }

        private static void BindBccServices(ContainerBuilder ioc)
        {
            ioc.Register(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var settings = resolver.Resolve<BaseSettings>();
                return new RpcConnectionParams(settings.Bcc);
            }).Keyed<RpcConnectionParams>(Constants.BccKey).SingleInstance();


            ioc.Register<Func<QBitNinjaClient>>(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                return () =>
                {
                    var settings = resolver.Resolve<BaseSettings>();
                    var connectionParams = resolver.Resolve<RpcConnectionParams>();
                    if (settings.Bcc.UseBccNinja)
                        return new QBitNinjaClient(settings.Bcc.QBitNinjaBaseUrl, connectionParams.Network);
                    return new QBitNinjaClient(settings.QBitNinjaBaseUrl, connectionParams.Network);
                };
            }).Keyed<Func<QBitNinjaClient>>(Constants.BccKey).SingleInstance();


            ioc.RegisterType<BccQBitNinjaApiCaller>().As<IBccQbBitNinjaApiCaller>().WithAttributeFiltering();

            ioc.RegisterType<BccOutputService>().As<IBccOutputService>().WithAttributeFiltering();

            ioc.RegisterType<RpcBccClient>().Keyed<IRpcBitcoinClient>(Constants.BccKey).WithAttributeFiltering();

            ioc.RegisterType<BccTransactionService>().As<IBccTransactionService>().WithAttributeFiltering();
        }

        private static void BindApiProviders(ContainerBuilder ioc)
        {
            ioc.Register(x =>
            {
                var client = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(5),
                    BaseAddress = new Uri("https://bitcoinfees.21.co/api/v1")
                };
                return RestClient.For<IFeeRateApiProvider>(client);
            }).As<IFeeRateApiProvider>().SingleInstance();

            ioc.Register(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var settings = resolver.Resolve<BaseSettings>();
                var client = new HttpClient
                {
                    BaseAddress = new Uri(settings.BitcoinCallbackUrl)
                };
                return RestClient.For<ILykkeApiProvider>(client);
            }).As<ILykkeApiProvider>().SingleInstance();
           
            ioc.Register(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var settings = resolver.Resolve<BaseSettings>();
                
                return RestClient.For<ISignatureApi>(new HttpClient { BaseAddress = new Uri(settings.SignatureProviderUrl) });
            }).As<ISignatureApi>().SingleInstance();

            ioc.RegisterType<SignatureApiProvider>().As<ISignatureApiProvider>().SingleInstance();
        }
    }
}
