using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;

using Microsoft.Extensions.DependencyInjection;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities.Moderation;

namespace PartnerBot.Core.Services
{
    public class GuildBanService
    {
        private readonly IServiceProvider _services;
        private readonly DiscordShardedClient _client;
        private readonly PartnerManagerService _partners;

        public GuildBanService(IServiceProvider services, DiscordShardedClient client,
            PartnerManagerService partners)
        {
            _services = services;
            _client = client;
            _partners = partners;
        }

        public async Task<GuildBan?> GetBanAsync(ulong guildId)
        {
            var db = _services.GetRequiredService<PartnerDatabaseContext>();
            return await db.FindAsync<GuildBan>(guildId);
        }

        public async Task<GuildBan> BanGuildAsync(ulong guildId, string? reason = null)
        {
            var db = _services.GetRequiredService<PartnerDatabaseContext>();
            var ban = await db.FindAsync<GuildBan>(guildId);

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
            var db = _services.GetRequiredService<PartnerDatabaseContext>();
            var ban = await db.FindAsync<GuildBan>(guildId);

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
            foreach(var shard in _client.ShardClients.Values)
            {
                if(shard.Guilds.TryGetValue(guildId, out var guild))
                {
                    var hookid = await _partners.GetPartnerElementAsync(guildId, x => x.WebhookId);

                    if (hookid != 0)
                    {
                        var hook = await shard.GetWebhookAsync(hookid);

                        await hook.ExecuteAsync(new DiscordWebhookBuilder()
                            .WithContent($"Your server has been banned from Partner Bot due to: {reasonToSend ?? "Violation of TOS"}\n\n" +
                            $"Contact a staff member on the support server at https://discord.gg/3SCTnhCMam to learn more."));

                        await hook.DeleteAsync();
                    }

                    await _partners.UpdateOrAddPartnerAsync(guildId, () => new()
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
