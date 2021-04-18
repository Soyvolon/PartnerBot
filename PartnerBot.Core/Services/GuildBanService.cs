using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;

using Microsoft.Extensions.DependencyInjection;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities.Moderation;

namespace PartnerBot.Core.Services
{
    /// <summary>
    /// The service that handls banning servers and keeping them banned.
    /// </summary>
    public class GuildBanService
    {
        private readonly IServiceProvider _services;
        private readonly DiscordShardedClient _client;
        private readonly PartnerManagerService _partners;

        public GuildBanService(IServiceProvider services, DiscordShardedClient client,
            PartnerManagerService partners)
        {
            this._services = services;
            this._client = client;
            this._partners = partners;
        }

        public async Task<GuildBan?> GetBanAsync(ulong guildId)
        {
            PartnerDatabaseContext? db = this._services.GetRequiredService<PartnerDatabaseContext>();
            return await db.FindAsync<GuildBan>(guildId);
        }

        public async Task<GuildBan> BanGuildAsync(ulong guildId, string? reason = null)
        {
            PartnerDatabaseContext? db = this._services.GetRequiredService<PartnerDatabaseContext>();
            GuildBan? ban = await db.FindAsync<GuildBan>(guildId);

            if (ban is null)
            {
                ban = new GuildBan()
                {
                    GuildId = guildId,
                    Reason = reason,
                    BanTime = DateTime.Now
                };

                await db.AddAsync(ban);
            }
            else
            {
                ban.Reason = reason;
                ban.BanTime = DateTime.Now;
            }

            await db.SaveChangesAsync();
            return ban;
        }

        public async Task<bool> UnbanGuildAsync(ulong guildId)
        {
            PartnerDatabaseContext? db = this._services.GetRequiredService<PartnerDatabaseContext>();
            GuildBan? ban = await db.FindAsync<GuildBan>(guildId);

            if (ban is not null)
            {
                db.Remove(ban);
                await db.SaveChangesAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task FinalizeBanAsync(ulong guildId, string? reasonToSend = null)
        {
            foreach(DiscordClient? shard in this._client.ShardClients.Values)
            {
                if(shard.Guilds.TryGetValue(guildId, out DiscordGuild? guild))
                {
                    ulong hookid = await this._partners.GetPartnerElementAsync(guildId, x => x.WebhookId);

                    if (hookid != 0)
                    {
                        DiscordWebhook? hook = await shard.GetWebhookAsync(hookid);

                        await hook.ExecuteAsync(new DiscordWebhookBuilder()
                            .WithContent($"Your server has been banned from Partner Bot due to: {reasonToSend ?? "Violation of TOS"}\n\n" +
                            $"Contact a staff member on the support server at https://discord.gg/3SCTnhCMam to learn more."));

                        await hook.DeleteAsync();
                    }

                    await this._partners.UpdateOrAddPartnerAsync(guildId, () => new()
                    {
                        Active = false,
                        ChannelId = 0,
                    });

                    await guild.LeaveAsync();
                }
            }
        }
    }
}
