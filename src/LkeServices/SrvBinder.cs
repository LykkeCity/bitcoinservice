using System;
using System.Net.Http;
using Autofac;
using Autofac.Features.AttributeFilters;
using Core;
using Core.Bcc;
using Core.Bitcoin;
using Core.Outputs;
using Core.Performance;
using Core.Providers;
using Core.QBitNinja;
using Core.RabbitNotification;
using Core.Settings;
using LkeServices.Bcc;
using LkeServices.Bitcoin;
using LkeServices.Multisig;
using LkeServices.Outputs;
using LkeServices.Performance;
using LkeServices.Providers;
using LkeServices.Providers.Rest;
using LkeServices.QBitNinja;
using LkeServices.RabbitNotifiaction;
using LkeServices.Signature;
using LkeServices.Transactions;
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
            ioc.RegisterType<OffchainService>().As<IOffchainService>();
            ioc.RegisterType<MultisigService>().As<IMultisigService>();
            ioc.RegisterType<TransactionBuildHelper>().As<ITransactionBuildHelper>();
            ioc.RegisterType<BitcoinTransactionService>().As<IBitcoinTransactionService>();
            ioc.RegisterType<OffchainService>().As<IOffchainService>();
            ioc.RegisterType<SignatureVerifier>().As<ISignatureVerifier>();
            ioc.RegisterType<BitcoinBroadcastService>().As<IBitcoinBroadcastService>();
            ioc.RegisterType<FailedTransactionsManager>().As<IFailedTransactionsManager>();
            ioc.RegisterType<PerformanceMonitorFactory>().As<IPerformanceMonitorFactory>();
            ioc.RegisterType<SpentOutputService>().As<ISpentOutputService>();

            ioc.Register(x =>
                {
                    var resolver = x.Resolve<IComponentContext>();
                    var settings = resolver.Resolve<BaseSettings>();
                    return new RabbitMqPublisher(settings.RabbitMq.ExplorerNotificationConnection.ConnectionString,
                                                 settings.RabbitMq.ExplorerNotificationConnection.Exchange);
                }).Named<IRabbitMqPublisher>(Constants.RabbitMqExplorerNotification).SingleInstance().AutoActivate();

            ioc.Register(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var settings = resolver.Resolve<BaseSettings>();
                return new RabbitMqPublisher(settings.RabbitMq.MultisigNotificationConnection.ConnectionString,
                    settings.RabbitMq.MultisigNotificationConnection.Exchange);
            }).Named<IRabbitMqPublisher>(Constants.RabbitMqMultisigNotification).SingleInstance().AutoActivate();


            ioc.Register<Func<string, IRabbitMqPublisher>>(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                return queue => resolver.ResolveNamed<IRabbitMqPublisher>(queue);
            });

            ioc.RegisterType<RabbitNotificationService>().As<IRabbitNotificationService>();

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
                    BaseAddress = new Uri(settings.LykkeJobsUrl)
                };
                return RestClient.For<ILykkeApiProvider>(client);
            }).As<ILykkeApiProvider>().SingleInstance();

            ioc.Register(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var settings = resolver.Resolve<BaseSettings>();
                return new HttpClient { BaseAddress = new Uri(settings.ClientSignatureProviderUrl) };
            }).Named<HttpClient>("client-signature-http").SingleInstance();

            ioc.Register(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var settings = resolver.Resolve<BaseSettings>();
                return new HttpClient { BaseAddress = new Uri(settings.SignatureProviderUrl) };
            }).Named<HttpClient>("server-signature-http").SingleInstance();

            ioc.Register<Func<SignatureApiProviderType, ISignatureApi>>(x =>
            {
                var resolver = x.Resolve<IComponentContext>();

                return type =>
                {
                    switch (type)
                    {
                        case SignatureApiProviderType.Client:
                            return RestClient.For<ISignatureApi>(resolver.ResolveNamed<HttpClient>("client-signature-http"));
                        case SignatureApiProviderType.Exchange:
                            return RestClient.For<ISignatureApi>(resolver.ResolveNamed<HttpClient>("server-signature-http"));
                        default:
                            throw new ArgumentException();
                    }
                };
            });

            ioc.Register<Func<SignatureApiProviderType, ISignatureApiProvider>>(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                var factory = resolver.Resolve<Func<SignatureApiProviderType, ISignatureApi>>();
                return type => new SignatureApiProvider(factory(type));
            });
        }
    }
}
