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
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())                
                .UseStartup<Startup>();

            if (arguments.ContainsKey("port"))
                builder.UseUrls($"http://*:{arguments["port"]}");

            Console.WriteLine($"Web Server is running");
            Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            var host = builder.Build();

            host.Run();
        }        
    }
}
