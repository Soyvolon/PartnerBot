using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using PartnerBot.Core.Services;

namespace PartnerBot.Discord.Commands.Utility
{
    public class InfoCommand : CommandModule
    {
        private readonly DiscordShardedClient _client;
        private readonly PartnerManagerService _partner;

        public InfoCommand(DiscordShardedClient client, PartnerManagerService partner)
        {
            _client = client;
            _partner = partner;
        }

        [Command("info")]
        [Description("Gets information about Partner Bot and your server.")]
        public async Task InfoCommandAsync(CommandContext ctx)
        {
            long guildCount = 0;
            long memberCount = 0;

            foreach (var shard in _client.ShardClients.Values)
            {
                guildCount += shard.Guilds.Count;
                foreach(var g in shard.Guilds.Values)
                {
                    memberCount += g.MemberCount;
                }
            }

            DiscordEmbedBuilder stats = new();
            stats.WithTitle("Partner Bot")
                .WithColor(Color_PartnerBotMagenta)
                .AddField("Global Stats:",
                    $"Total Servers: {guildCount}\n" +
                    $"Maximum Members Reached: {memberCount}")
                .AddField($"{ctx.Guild.Name} Info",
                    $"Shard: {ctx.Client.ShardId}\n" +
                    $"Active: {await _partner.GetPartnerElementAsync(ctx.Guild.Id, x => x.Active)}")
                .AddField($"Developer(s):",
                    "Soyvolon")
                .AddField($"Contributor(s):",
                    $"Neuheit\n\n" +
                    $"*Want to help out? Check out our [GitHub]()*")
                .WithFooter($"{ctx.Prefix}info")
                .WithTimestamp(DateTime.Now);

            await ctx.RespondAsync(stats);
        }
    }
}
