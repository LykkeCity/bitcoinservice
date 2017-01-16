using System;
using System.Collections.Generic;
using System.Linq;
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
using LkeServices.QBitNinja;
using LkeServices.Signature;
using LkeServices.Transactions;
using PhoneNumbers;
using QBitNinja.Client;
using RestSharp;

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
            ioc.Register(x => new RestClient()).As<IRestClient>();

            ioc.RegisterType<SignatureApiProvider>().As<ISignatureApiProvider>().SingleInstance();
            ioc.RegisterType<LykkeApiProvider>().As<ILykkeApiProvider>().SingleInstance();
            ioc.RegisterType<FeeRateApiProvider>().As<IFeeRateApiProvider>().SingleInstance();

            ioc.RegisterType<LykkeTransactionBuilderService>().As<ILykkeTransactionBuilderService>();
            ioc.RegisterType<OffchainTransactionBuilderService>().As<IOffchainTransactionBuilderService>();
            ioc.RegisterType<MultisigService>().As<IMultisigService>();
            ioc.RegisterType<TransactionBuildHelper>().As<ITransactionBuildHelper>();
            ioc.RegisterType<BitcoinTransactionService>().As<IBitcoinTransactionService>();
            ioc.RegisterType<OffchainTransactionBuilderService>().As<IOffchainTransactionBuilderService>();
            ioc.RegisterType<SignatureVerifier>().As<ISignatureVerifier>();
            ioc.RegisterType<BitcoinBroadcastService>().As<IBitcoinBroadcastService>();
        }
    }
}
