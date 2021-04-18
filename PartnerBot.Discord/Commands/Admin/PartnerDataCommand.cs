using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using Microsoft.Extensions.DependencyInjection;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;
using PartnerBot.Core.Services;
using PartnerBot.Discord.Commands.Conditions;

namespace PartnerBot.Discord.Commands.Admin
{
    public class PartnerDataCommand : CommandModule
    {
        private readonly IServiceProvider _services;
        private readonly DiscordShardedClient _client;
        private readonly GuildBanService _ban;

        public PartnerDataCommand(IServiceProvider services, DiscordShardedClient client,
            GuildBanService ban)
        {
            this._services = services;
            this._client = client;
            this._ban = ban;
        }

        [Command("data")]
        [Description("Gets the database information for a guild")]
        [RequireCessumStaff]
        public async Task PartnerDataCommandAsync(CommandContext ctx, ulong guildId)
        {
            PartnerDatabaseContext? db = this._services.GetRequiredService<PartnerDatabaseContext>();
            Partner? partner = await db.FindAsync<Partner>(guildId);

            DiscordGuild? guild = null;
            foreach (DiscordClient? shard in this._client.ShardClients.Values)
            {
                if (shard.Guilds.TryGetValue(guildId, out guild))
                    break;
            }

            if (guild is null && partner is null)
                await RespondError("No data found for that ID");

            if (guild is not null)
            {
                DiscordEmbedBuilder cacheinfo = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Azure)
                    .WithTitle("Guild Cache Info:")
                    .WithImageUrl(guild.IconUrl)
                    .AddField("Guild Name: ", guild.Name, true)
                    .AddField("Guild ID: ", guild.Id.ToString(), true)
                    .AddField("Guild Size: ", guild.MemberCount.ToString(), true)
                    .AddField("Owner Name:", $"{guild.Owner.Username}#{guild.Owner.Discriminator}", true)
                    .AddField("Owner ID: ", guild.Owner.Id.ToString(), true)
                    .AddField("Owner Mention: ", guild.Owner.Mention, true)
                    .AddField("Connected on Shard: ", this._client.ShardClients.Values.Where(c => c.Guilds.ContainsKey(guild.Id)).First().ShardId.ToString(), true);
                
                try
                {
                    IReadOnlyList<DiscordInvite> invites = await guild.GetInvitesAsync().ConfigureAwait(false);

                    DiscordInvite? i = invites.FirstOrDefault(i => !i.IsRevoked);

                    if (i is null)
                        i = await guild.GetDefaultChannel().CreateInviteAsync();

                    cacheinfo.AddField("Invite: ", i.ToString(), true);
                }
                catch
                {
                    cacheinfo.AddField("Invite: ", "Invalid Permissions", true);
                }

                await ctx.RespondAsync(cacheinfo);
            }

            if(partner is not null)
            {
                DiscordEmbedBuilder dbInfo = new();

                dbInfo.AddField("Guild ID: ", partner.GuildId.ToString(), true)
                    .AddField("Owner ID: ", partner.OwnerId.ToString(), true)
                    .AddField("Toggled?", partner.Active.ToString(), true)
                    .AddField("Donor Rank: ", $"{partner.DonorRank}", true)
                    .AddField("NSFW: ", partner.NSFW.ToString(), true)
                    .AddField("Get NSFW: ", partner.ReceiveNSFW.ToString(), true)
                    .AddField("Webhook ID", $"{partner.WebhookId}", true)
                    .WithImageUrl(partner.Banner)
                    .WithAuthor(partner.GuildName, null, partner.GuildIcon)
                    .WithDescription($"**Message:** \n\n{partner.Message}\n\n")
                    .WithTitle("Guild Database Info")
                    .WithFooter($"{ctx.Prefix}data")
                    .WithTimestamp(DateTime.Now);

                await ctx.RespondAsync(dbInfo);
            }

            PartnerBot.Core.Entities.Moderation.GuildBan? ban = await this._ban.GetBanAsync(guildId);

            if(ban is not null)
            {
                DiscordEmbedBuilder banInfo = new();

                banInfo.WithTitle("Ban Info")
                    .AddField("Reason", ban.Reason ?? "None Given")
                    .AddField("Ban Time", ban.BanTime.ToString("U"));
            }
        }
    }
}
