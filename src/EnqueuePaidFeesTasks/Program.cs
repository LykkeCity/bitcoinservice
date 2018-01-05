using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnqueuePaidFeesTasks
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

            provider.GetService<Job>().Report().Wait();
        }
    }
}
