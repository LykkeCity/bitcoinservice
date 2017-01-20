using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace EnqueueFees
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var arguments = args.Select(t => t.Split('=')).ToDictionary(spl => spl[0].Trim('-'), spl => spl[1]);

            if (!arguments.ContainsKey("type") ||
                (arguments["type"] != "fee" && arguments["type"] != "asset") ||
                (arguments["type"] == "asset" && !arguments.ContainsKey("asset")))
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("-type=[fee|asset]");
                Console.WriteLine("use parameter -asset=[assetId] for -type=asset");
                Console.WriteLine("Press Enter for exit");
                Console.ReadLine();
                return;
            }

            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            var provider = DependencyBinder.BindAndBuild(configuration);
            try
            {
                provider.GetService<EnqueueFeesJob>().Start(arguments["type"], arguments.ContainsKey("asset") ? arguments["asset"] : null).Wait();
                Console.WriteLine("All queues are updated. Press Enter to exit");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal Exception: " + ex.Message + " " + ex.StackTrace);
                Console.ReadLine();
            }
        }
    }
}
