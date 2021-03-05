using System;
using System.Diagnostics;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace PartnerBot.Discord.Commands.Utility
{
    public class PingCommand : CommandModule
    {
        [Command("ping")]
        [Description("Get the ping and latency of the bot.")]
        public async Task PingCommandAsync(CommandContext ctx)
        {
            Stopwatch timer = new Stopwatch();
            var pingEmbed = new DiscordEmbedBuilder()
                .WithTitle($"Ping for Shard {ctx.Client.ShardId}")
                .WithColor(new DiscordColor(0x08423ec))
                .WithFooter($"{ctx.Prefix}{ctx.Command.Name}")
                .WithTimestamp(DateTime.Now)
                .AddField("WS Latency:", $"{ctx.Client.Ping}ms");
            timer.Start();
            DiscordMessage msg = await ctx.RespondAsync(pingEmbed);
            await msg.ModifyAsync(null, pingEmbed
                .AddField("Response Time: (:ping_pong:)", $"{timer.ElapsedMilliseconds}ms")
                .Build());
            timer.Stop();
        }
    }
}
