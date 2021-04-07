using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;

using Microsoft.Extensions.Logging;

using PartnerBot.Core.Entities.Configuration;
using PartnerBot.Core.Services;
using PartnerBot.Discord.Services;

namespace PartnerBot.Discord
{
    public class DiscordBot
    {
        #region Event Ids
        // 127### - designates a Discord Bot event.
        public static EventId Event_CommandErrorHandler { get; } = new EventId(127001, "Command Error Handler");
        public static EventId Event_CommandHandler { get; } = new EventId(127002, "Command Handler");
        public static EventId Event_ClientLogger { get; } = new EventId(127003, "Client Logger");
        #endregion

        #region Attribute Only Values
        public static PartnerBotConfiguration? PbCfg { get; private set; } = null;
        public static DiscordShardedClient? Client { get; private set; } = null;
        public static IServiceProvider? Services { get; private set; } = null;
        #endregion

        private readonly PartnerSenderService _partnerSender;
        private readonly DiscordShardedClient _client;
        private readonly DiscordRestClient _rest;
        private readonly CommandErrorHandlingService _error;
        private readonly CommandHandlingService _command;
        private readonly GuildVerificationService _verify;
        private readonly IServiceProvider _serviceProvider;

        private Timer PartnerTimer { get; set; }

        public DiscordBot(PartnerSenderService partnerSender,
            DiscordShardedClient client, DiscordRestClient rest,
            CommandErrorHandlingService error, CommandHandlingService command,
            GuildVerificationService verify, PartnerBotConfiguration pcfg,
            IServiceProvider serviceProvider)
        {
            _partnerSender = partnerSender;
            _client = client;
            _rest = rest;
            _error = error;
            _command = command;
            _verify = verify;
            _serviceProvider = serviceProvider;

            Client = _client;
            PbCfg = pcfg;
            Services = _serviceProvider;
        }

        public async Task InitalizeAsync()
        {
            _client.MessageCreated += _command.Client_MessageCreated;
            _client.Ready += Client_Ready;

            var cnext = await _client.UseCommandsNextAsync(GetCNextConfig());

            foreach(var c in cnext.Values)
            {
                c.RegisterCommands(Assembly.GetAssembly(typeof(CommandHandlingService)));

                c.CommandErrored += _error.Client_CommandErrored;
                c.CommandExecuted += _error.Client_CommandExecuted;
            }

            var interact = await _client.UseInteractivityAsync(new());

            await _rest.InitializeAsync();

            await _verify.InitalizeAsync();
        }

        private Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            _ = Task.Run(() =>
            {
                _verify.Start();
                PartnerTimer = new(OnPartnerRunTimer, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            });

            sender.Logger.LogInformation("Client Ready");

            return Task.CompletedTask;
        }

        public async Task StartAsync()
        {
            await _client.StartAsync();
        }

        private CommandsNextConfiguration GetCNextConfig()
        {
            return new()
            {
                UseDefaultCommandHandler = false,
                Services = _serviceProvider,
            };
        }

        #region Partner Running
        private int lastHour = -1;
        private bool firstRun = false;
        private bool secondRun = false;
        private bool thirdRun = false;

        private void OnPartnerRunTimer(object? data)
        {
            var chour = DateTime.UtcNow.Hour;
            if (chour > lastHour)
            {
                var min = DateTime.UtcNow.Minute;

                if(min >= 0 && !firstRun)
                {
                    _ = Task.Run(async () => await _partnerSender.ExecuteAsync(new()
                    {
                        DonorRun = 0
                    }));

                    firstRun = true;
                }
                else if(min >= 15 && !secondRun)
                {
                    _ = Task.Run(async () => await _partnerSender.ExecuteAsync(new()
                    {
                        DonorRun = 1
                    }));

                    secondRun = true;
                }
                else if(min >= 30 && !thirdRun)
                {
                    _ = Task.Run(async () => await _partnerSender.ExecuteAsync(new()
                    {
                        DonorRun = 2
                    }));

                    thirdRun = true;
                }
                else
                {
                    _ = Task.Run(async () => await _partnerSender.ExecuteAsync(new()
                    {
                        DonorRun = 3
                    }));

                    lastHour = chour;
                    firstRun = false;
                    secondRun = false;
                    thirdRun = false;
                }
            }
        }

        #endregion
    }
}
