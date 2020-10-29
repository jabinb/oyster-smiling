using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OysterSmiling.WorkerServices
{
    public class PhraseService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PhraseService> _logger;

        private readonly Dictionary<string, Func<DiscordMessage, Task>> _phraseHandlers = 
            new Dictionary<string, Func<DiscordMessage, Task>>
        {
            ["Can I get a printout of oyster smiling?"] = OnOysterPrintout
        };

        public PhraseService(IServiceProvider serviceProvider, ILogger<PhraseService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            
            using (var scope = _serviceProvider.CreateScope())
            {
                _logger.LogInformation("Watching for phrases.");
                var inter = scope.ServiceProvider.GetService<InteractivityModule>();

                if (inter == null)
                {
                    throw new ArgumentNullException(nameof(inter));
                }
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    await inter.WaitForMessageAsync(OnMessage, TimeSpan.FromSeconds(60));
                }
            }
        }

        private bool OnMessage(DiscordMessage message)
        {
            var found = false;
            var tasks = new List<Task>();
            foreach (var (phrase, handler) in _phraseHandlers)
            {
                if (message.Content.Contains(phrase, StringComparison.InvariantCultureIgnoreCase))
                {
                    tasks.Add(handler(message));
                    found = true;
                }
            }

            if (tasks.Count > 0)
                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));

            return found;
        }

        private static async Task OnOysterPrintout(DiscordMessage message)
        {
            await message.RespondAsync("Uhh.. okay");
            await using (var jpg = File.OpenRead("./oyster_smiling.jpg"))
            {
                await message.RespondWithFileAsync(jpg);
            }
        }
    }
}