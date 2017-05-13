using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories;
using BitcoinApi;
using Core.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ApiRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var arguments = args.Select(t => t.Split('=')).ToDictionary(spl => spl[0].Trim('-'), spl => spl[1]);

            Console.Clear();
            Console.Title = "Bitcoin self-hosted API - Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

            var builder = new WebHostBuilder()
                .UseKestrel(o => o.ThreadCount = 4)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();

            if (arguments.ContainsKey("port"))
                builder.UseUrls($"http://*:{arguments["port"]}");

            Console.WriteLine($"Web Server is running");
            Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            var host = builder.Build();

            host.Run();
        }

        static void Exit()
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static BaseSettings GetSettings()
        {
            var settingsData = ReadSettingsFile();

            if (string.IsNullOrWhiteSpace(settingsData))
            {
                Console.WriteLine("Please, provide generalsettings.json file");
                return null;
            }

            var settings = JsonConvert.DeserializeObject<BaseSettings>(settingsData);

            return settings;
        }


        static string ReadSettingsFile()
        {
#if DEBUG
            return File.ReadAllText(@"..\..\settings\settings.json");
#else
			return File.ReadAllText("settings.json");
#endif
        }

    }
}
