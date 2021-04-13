using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;

using PartnerBot.Core.Entities;
using PartnerBot.Core.Utils;

namespace PartnerBot.Discord.Commands.Testing
{
    public partial class TestCommandGroup : CommandModule
    {
        private static readonly List<string> Names = new List<string>() {
"Kuornos", "Ilmadia", "Iarmenor", "Mithrandir", "Baraion", "Inarie", "Virtheon", "Maeralya", "Maeglin", "Oritris", "Tipandon", "Ithirae", "Tamnaeuth", "Ornthalas",
"Eilana", "Baradan", "lrune", "Doluilos", "Adanlas", "Dior", "Ardorius", "Edhelgil", "Ellania", "Beala", "Aranadar", "Nesterin", "Sharian", "Myrddin", "Nuesti",
"Findir", "Char", "Gyo", "Gol", "Khmaz", "Apallon", "Yolon", "Elpys", "Vasuki", "Dreq", "Ruu", "Korazion", "Nazalath", "Thrarion", "Drakus", "Drayke", };

        private static readonly List<string> Tags = new List<string>()
        {
            "sports", "games", "outside", "late-night", "soccer", "football", "csgo", "minecraft", "dota", "lol", "warcraft", "satisfactory", "game-development", "bot-developemnt",
            "home", "reading", "books", "fanfiction", "life", "irl", "no-sleep", "fanart", "among-us", "disney", "eevee", "water-bottle", "scissors", "desert",
            "hairband", "sunglsses", "chocolate", "microphone", "plants", "power", "plushies", "sleep"
        };

        [Command("setup")]
        public async Task SetupTestServers(CommandContext ctx, params DiscordGuild[] guilds)
        {
            List<string> names = new();
            foreach (var g in guilds)
                names.Add(g.Name);

            var interact = ctx.Client.GetInteractivity();

            var embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle("Partner Test Server Setup")
                .WithDescription("Setting up Partner Bot Test Server. Please ensure the bot has admin on all servers. Type `continue` to start.\n\n" +
                "**THIS WILL DELETE ALL CONTENT FROM THE FOLLOWING GUILDS:**")
                .AddField("Guilds: ", string.Join(",", names));

            var message = await ctx.RespondAsync(embed);

            var response = await interact.WaitForMessageAsync(x => x.Channel.Id == ctx.Channel.Id && x.Author.Id == ctx.Member.Id);

            if (response.TimedOut)
            {
                await InteractTimeout();
                return;
            }
            else if(!response.Result.Content.ToLower().Equals("continue"))
            {
                await RespondError("Aborting...");
                return;
            }

            var queue = new Queue<DiscordGuild>(guilds);

            var data = await DevelopmentStressTestDataManager.GetDataAsync();

            if (data is null) data = new();

            DiscordMessage? msg = null;
            while (queue.TryDequeue(out var guild))
            {
                int channelStage = 0;
                int webhookStage = 0;
                msg = await Update(ctx, guild, queue, true, channelStage, webhookStage, msg);

                await CleanupGuild(guild);

                var baseChan = new DevelopmentStressTestChannel()
                {
                    GuildId = guild.Id,
                    GuildName = Names[ThreadSafeRandom.Next(0, Names.Count)]
                };

                await guild.ModifyAsync(x => x.Name = $"Test Server {baseChan.GuildName}");

                msg = await Update(ctx, guild, queue, false, channelStage, webhookStage, msg);

                List<DevelopmentStressTestChannel> createdChannels = new();
                List<DiscordChannel> rawChannels = new();
                // build new channels
                int x = 1;
                for (int i = 0; i < 50; i++)
                {
                    if (x % 5 == 0)
                        msg = await Update(ctx, guild, queue, false, ++channelStage, webhookStage, msg);

                    var chan = await BuildChannel(guild, baseChan);

                    createdChannels.Add(chan.Item1);
                    rawChannels.Add(chan.Item2);

                    x++;

                    await Task.Delay(TimeSpan.FromSeconds(.5));
                }

                msg = await Update(ctx, guild, queue, false, channelStage, webhookStage, msg);

                // build new webhooks
                x = 1;
                int rc = 0;
                foreach(var c in createdChannels)
                {
                    if(x % 5 == 0)
                        msg = await Update(ctx, guild, queue, false, channelStage, ++webhookStage, msg);

                    await ApplyWebhookData(c, rawChannels[rc++]);

                    x++;

                    await Task.Delay(TimeSpan.FromSeconds(.5));
                }

                // save data
                data.AddRange(createdChannels);
            }

            await msg.ModifyAsync(embed: new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Orange)
                .WithTitle("Partner Test Server Cleanup")
                .WithDescription("Cleaning up test server data...")
                .Build());

            data.RemoveAll(x =>
            {
                bool found = false;
                foreach(var shard in _client.ShardClients.Values)
                {
                    if(shard.Guilds.ContainsKey(x.GuildId))
                    {
                        found = true;
                        break;
                    }
                }

                return !found;
            });

            await DevelopmentStressTestDataManager.SaveDataAsync(data);

            await RespondSuccess("Setup Complete.");
        }

        private async Task ApplyWebhookData(DevelopmentStressTestChannel dev, DiscordChannel c)
        {
            var hook = await c.CreateWebhookAsync("Partner Test Sender", reason: "Partner Test Server Setup");

            dev.WebhookId = hook.Id;
            dev.WebhookToken = hook.Token;
        }

        private async Task<(DevelopmentStressTestChannel, DiscordChannel)> BuildChannel(DiscordGuild g, DevelopmentStressTestChannel dev)
        {
            var chan = new DevelopmentStressTestChannel()
            {
                GuildId = dev.GuildId,
                GuildName = dev.GuildName,
                Tags = GetTags(),
                Name = $"{Names[ThreadSafeRandom.Next(0, Names.Count)]}-{Names[ThreadSafeRandom.Next(0, Names.Count)]}-{dev.GuildName}"
            };

            var c = await g.CreateTextChannelAsync(chan.Name, topic: $"Tags: {string.Join(", ", chan.Tags)}", reason: "Partner Test Server Setup");

            chan.ChannelId = c.Id;

            return (chan, c);
        }

        private List<string> GetTags()
        {
            var amnt = ThreadSafeRandom.Next(0, 11);
            List<string> tags = new();
            for(int i = 0; i < amnt; i++)
            {
                tags.Add(Tags[ThreadSafeRandom.Next(0, tags.Count)]);
            }

            return tags;
        }

        private async Task CleanupGuild(DiscordGuild g)
        {
            foreach(var chan in g.Channels)
            {
                await chan.Value.DeleteAsync("Partner Test Server Setup");
                await Task.Delay(TimeSpan.FromSeconds(.5));
            }
        }

        private async Task<DiscordMessage> Update(CommandContext ctx, DiscordGuild guild, Queue<DiscordGuild> queue, bool cleanup, int channelStage, int webhookStage, DiscordMessage? msg = null)
        {
            var embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Orange)
                .WithTitle("Partner Test Server Setup");

            embed = GetUpdateData(embed, guild, queue, cleanup, channelStage, webhookStage);

            if (msg is null)
                return await ctx.RespondAsync(embed);
            else
                return await msg.ModifyAsync(null, embed.Build());
        }

        private DiscordEmbedBuilder GetUpdateData(DiscordEmbedBuilder b, DiscordGuild guild, Queue<DiscordGuild> queue, bool cleanup, int channelStage, int webhookStage)
        {
            string data;

            if (cleanup)
                data = "**Cleaning Old Channels...**";
            else
            {
                data = "**Adding Channels:**\n" +
                    "`[";
                for(int i = 0; i < 10; i++)
                {
                    if (i < channelStage)
                        data += "X";
                    else data += "_";
                }

                data += "]`\n\n" +
                    "**Adding Webhooks:**\n" +
                    "`[";
                for (int i = 0; i < 10; i++)
                {
                    if (i < webhookStage)
                        data += "X";
                    else data += "_";
                }

                data += "]`";
            }

            b.WithDescription("**Remaining Servers:**\n" +
                    "```\n" +
                    $"{string.Join(", ", queue)}" +
                    "\n```")
                .AddField($"Current Guild: {guild.Name}", 
                    data);

            return b;
        }
    }
}
