using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandDotNet;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OysterSmiling.Commands;
using OysterSmiling.Models;

namespace OysterSmiling
{
    public class OysterMain
    {
        private readonly ILogger<OysterMain> _logger;
        private readonly BotSettings _settings;
        
        private readonly DiscordClient _client;
        private readonly InteractivityModule _interactivity;
        
        public OysterMain(
            IOptions<BotSettings> options, 
            ILogger<OysterMain> logger,
            DiscordClient client,
            InteractivityModule interactivity
        )
        {
            _logger = logger;
            _client = client;
            _interactivity = interactivity;
            _settings = options.Value;
        }
        
        [DefaultMethod]
        public async Task Run(CancellationToken cancellationToken)
        {
            Startup();

            // Keep the bot running until the cancellation token requests we stop
            while (!cancellationToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
        
        private void Startup() 
        {
            try
            {
                using var deps = new DependencyCollectionBuilder()
                    .AddInstance(_interactivity)
                    .AddInstance(_client);
                
                var commands = _client.UseCommandsNext(new CommandsNextConfiguration
                {
                    StringPrefix = _settings.CommandPrefix,
                    Dependencies = deps.Build()
                });

                _logger.LogInformation("[info] Loading command modules..");

                var type = typeof(ICommand);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes()) 
                    .Where(p => type.IsAssignableFrom(p) && !p.IsInterface); 

                var typeList = types as Type[] ?? types.ToArray(); // Convert to an array
                foreach (var t in typeList)
                    commands.RegisterCommands(t); // Loop through the list and register each command module with CommandsNext

                _logger.LogInformation($"[info] Loaded {typeList.Count()} modules.");
            }
            catch (Exception ex)
            {
                // This will catch any exceptions that occur during the operation/setup of your bot.

                // Feel free to replace this with what ever logging solution you'd like to use.
                // I may do a guide later on the basic logger I implemented in my most recent bot.
                Console.Error.WriteLine(ex.ToString());
            }
        }

    }
}