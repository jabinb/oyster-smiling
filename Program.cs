using System;
using System.IO;
using System.Threading.Tasks;
using CommandDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CommandDotNet.IoC.MicrosoftDependencyInjection;
using DSharpPlus;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OysterSmiling.Models;
using OysterSmiling.WorkerServices;

namespace OysterSmiling
{
    internal class Program
    {
        
        public static void Main(string[] args)
        {
            // create service collection
            var builder = Host.CreateDefaultBuilder(args).ConfigureServices(ConfigureServices).Build();
            
            var appRunner = new AppRunner<OysterMain>();
            appRunner.UseDefaultMiddleware();
            appRunner.UseMicrosoftDependencyInjection(builder.Services);
            
            // run app
            Task.WaitAll(builder.RunAsync(), appRunner.RunAsync(args));

        }
        private static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            // add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
            
            // build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true, true)
                .Build();

            services.AddOptions();
            services.Configure<BotSettings>(configuration.GetSection("discord"));

            
            // build services

            services.AddSingleton(p =>
            {
                var options = p.GetService<IOptions<BotSettings>>();
                var logger = p.GetService<ILogger<DiscordClient>>();

                var client = new DiscordClient(new DiscordConfiguration
                {
                    Token = options!.Value.Token,
                    TokenType = TokenType.Bot
                });
                // Connect to discord's service
                logger.LogInformation("Connecting..");
                client.ConnectAsync().Wait();
                logger.LogInformation("Connected!");

                return client;
            });

            services.AddSingleton(p =>
            {
                var discord = p.GetService<DiscordClient>();

                return discord.UseInteractivity(new InteractivityConfiguration
                {
                    PaginationBehaviour = TimeoutBehaviour.Delete, // What to do when a pagination request times out
                    PaginationTimeout = TimeSpan.FromSeconds(30), // How long to wait before timing out
                    Timeout = TimeSpan.FromSeconds(30)
                });
            });

            services.AddSingleton<PhraseService>();
            services.AddSingleton<IHostedService>(p => p.GetService<PhraseService>());
            
            services.AddTransient<OysterMain, OysterMain>();
        }
    }
}