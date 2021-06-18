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
            this._client = client;
            this._partner = partner;
        }

        [Command("info")]
        [Description("Gets information about Partner Bot and your server.")]
        public async Task InfoCommandAsync(CommandContext ctx)
        {
            long guildCount = 0;
            long memberCount = 0;

            foreach (DiscordClient? shard in this._client.ShardClients.Values)
            {
                guildCount += shard.Guilds.Count;
                foreach(DiscordGuild? g in shard.Guilds.Values)
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
                    $"Active: {await this._partner.GetPartnerElementAsync(ctx.Guild.Id, x => x.Active)}")
                .AddField($"Developer(s):",
                    "Soyvolon")
                .AddField($"Contributor(s):",
                    $"Neuheit\n\n" +
                    $"*Want to help out? Check out our [GitHub](https://github.com/Soyvolon/PartnerBot)*")
                .WithFooter($"{ctx.Prefix}info")
                .WithTimestamp(DateTime.Now);

            await ctx.RespondAsync(stats);
        }
    }
}
