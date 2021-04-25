using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public static EventId Event_CommandErrorHandler { get; } = new EventId(701, "CmdErrHandler");
        public static EventId Event_CommandHandler { get; } = new EventId(702, "CmdHandler");
        public static EventId Event_ClientLogger { get; } = new EventId(703, "ClientLogger");
        public static EventId Event_ShardBooter { get; } = new EventId(704, "ShardBooter");
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
            this._partnerSender = partnerSender;
            this._client = client;
            this._rest = rest;
            this._error = error;
            this._command = command;
            this._verify = verify;
            this._serviceProvider = serviceProvider;

            Client = this._client;
            PbCfg = pcfg;
            Services = this._serviceProvider;
        }

        public async Task InitalizeAsync()
        {
            this._client.MessageCreated += this._command.Client_MessageCreated;
            this._client.Ready += Client_Ready;

            System.Collections.Generic.IReadOnlyDictionary<int, CommandsNextExtension>? cnext = await this._client.UseCommandsNextAsync(GetCNextConfig());

            foreach(CommandsNextExtension? c in cnext.Values)
            {
                c.RegisterCommands(Assembly.GetAssembly(typeof(CommandHandlingService)));

                c.CommandErrored += this._error.Client_CommandErrored;
                c.CommandExecuted += this._error.Client_CommandExecuted;
            }

            System.Collections.Generic.IReadOnlyDictionary<int, DSharpPlus.Interactivity.InteractivityExtension>? interact = await this._client.UseInteractivityAsync(new());

            await this._rest.InitializeAsync();

            await this._verify.InitalizeAsync();
        }

        private Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            _ = Task.Run(() =>
            {
                this._verify.Start();
                this.PartnerTimer = new(OnPartnerRunTimer, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            });

            sender.Logger.LogInformation("Client Ready");

            return Task.CompletedTask;
        }

        private CommandsNextConfiguration GetCNextConfig()
        {
            return new()
            {
                UseDefaultCommandHandler = false,
                Services = _serviceProvider,
            };
        }

        #region Shard Loading
        private List<List<DiscordClient>> Buckets { get; set; }
        private const int DOWNLOAD_CONCURRENCY = 4;

        public async Task StartAsync()
        {
            int shardCount = PbCfg!.ShardCount == 1 ? _client.GatewayInfo.ShardCount : PbCfg.ShardCount;
            int concurrency = _client.GatewayInfo.SessionBucket.MaxConcurrency;

            Buckets = new();

            int b = -1;
            for(int i = 0; i < shardCount; i++)
            {
                if (i % concurrency == 0)
                {
                    Buckets.Add(new());
                    b++;
                }

                Buckets[b].Add(_client.ShardClients[i]);
            }

            this._client.Logger.LogInformation(Event_ShardBooter, $"Built {Buckets.Count} buckets for {shardCount} shards.");

            await BootBuckets();
        }

        private async Task BootBuckets()
        {
            int c = 0;
            foreach(var bucket in Buckets)
            {
                this._client.Logger.LogInformation(Event_ShardBooter, $"Booting bucket {c++}");

                int con = 0;
                List<DiscordClient> nextRun = new();
                for(int i = 0; i < bucket.Count; i++)
                {
                    nextRun.Add(bucket[i]);

                    if(con++ >= DOWNLOAD_CONCURRENCY)
                    {
                        con = 0;
                        await BootBatch(nextRun);
                        nextRun.Clear();
                    }
                }

                if(nextRun.Count > 0)
                    await BootBatch(nextRun);
            }
        }

        private async Task BootBatch(List<DiscordClient> clients)
        {
            int completed = 0;
            int running = clients.Count;

            foreach(var c in clients)
            {
                c.GuildDownloadCompleted += (c, e) =>
                {
                    if(clients.Contains(c))
                    {
                        c.Logger.LogInformation(Event_ShardBooter, $"Guild Download for shard {c.ShardId} completed.");
                        completed++;
                    }

                    return Task.CompletedTask;
                };
                c.Ready += Client_Ready;

                await c.ConnectAsync();
            }

            while(completed < running)
            {
                await Task.Delay(1000);
            };
        }
        #endregion

        #region Partner Running
        private int lastHour = -1;
        private bool firstRun = false;
        private bool secondRun = false;
        private bool thirdRun = false;

        private void OnPartnerRunTimer(object? data)
        {
            int chour = DateTime.UtcNow.Hour;
            if (chour != this.lastHour)
            {
                int min = DateTime.UtcNow.Minute;

                if(min >= 0 && min < 15 && !this.firstRun)
                {
                    _ = Task.Run(async () => await this._partnerSender.ExecuteAsync(new()
                    {
                        DonorRun = 0
                    }));

                    this.firstRun = true;
                }
                else if(min >= 15 && min < 30 && !this.secondRun)
                {
                    _ = Task.Run(async () => await this._partnerSender.ExecuteAsync(new()
                    {
                        DonorRun = 1
                    }));

                    this.secondRun = true;
                }
                else if(min >= 30 && min < 45 && !this.thirdRun)
                {
                    _ = Task.Run(async () => await this._partnerSender.ExecuteAsync(new()
                    {
                        DonorRun = 2
                    }));

                    this.thirdRun = true;
                }
                else if (min >= 45)
                {
                    _ = Task.Run(async () => await this._partnerSender.ExecuteAsync(new()
                    {
                        DonorRun = 3
                    }));

                    this.lastHour = chour;
                    this.firstRun = false;
                    this.secondRun = false;
                    this.thirdRun = false;
                }
            }
        }

        #endregion
    }
}
