using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OffchainRequestCreator;

namespace PoisonMessagesReenqueue
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                              .SetBasePath(Directory.GetCurrentDirectory())
                              .AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            var provider = DependencyBinder.BindAndBuild(configuration);

            provider.Resolve<Job>().Start().Wait();
        }
    }
}
