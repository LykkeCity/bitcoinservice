using System.IO;
using Autofac;
using Microsoft.Extensions.Configuration;
using PoisonMessagesReenqueue;

namespace ExportBccBalances
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
