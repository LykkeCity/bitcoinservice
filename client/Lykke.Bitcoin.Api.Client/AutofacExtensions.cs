using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Lykke.Bitcoin.Api.Client.BitcoinApi;

namespace Lykke.Bitcoin.Api.Client
{
    public static class AutofacExtensions
    {
       public static void RegisterBitcoinApiClient(this ContainerBuilder builder, string serviceUrl)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (serviceUrl == null) throw new ArgumentNullException(nameof(serviceUrl));
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            builder.Register<IBitcoinApiClient>(x => new BitcoinApiClient(serviceUrl)).SingleInstance();
        }

        public static void RegisterBitcoinApiClient(this ContainerBuilder builder, BitcoinServiceClientSettings settings)
        {
            RegisterBitcoinApiClient(builder, settings?.ServiceUrl);
        }
    }
}
