using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

using PartnerBot.Core.Entities.Configuration;

namespace PartnerBot.Core.Services
{
    /// <summary>
    /// The service that handles donor related status checking
    /// </summary>
    public class DonorService
    {
        public const int HIGHEST_RANK = 3;

        public const int EMBED_LIMIT = 2;
        public const int QUADRUPLE_EMBEDS = 4;
        public const int TRIPPLE_EMBEDS = 1;

        public const int VANITY_LIMIT = 1;

        private readonly DiscordShardedClient _client;
        private readonly PartnerBotConfiguration _pcfg;
        private readonly DiscordRestClient _rest;

        private DiscordGuild? HomeGuild = null;

        public DonorService(DiscordShardedClient client, PartnerBotConfiguration pcfg,
            DiscordRestClient rest)
        {
            this._client = client;
            this._pcfg = pcfg;
            this._rest = rest;
        }

        public async Task<int> GetDonorRankAsync(ulong ownerId)
        {
            if(this.HomeGuild is null)
            {
                foreach (DiscordClient? shard in this._client.ShardClients.Values)
                {
                    if (shard.Guilds.TryGetValue(this._pcfg.HomeGuild, out this.HomeGuild))
                    {
                        this.HomeGuild = await shard.GetGuildAsync(this._pcfg.HomeGuild);
                        break;
                    }
                }
            }

            if (this.HomeGuild is not null)
            {
                try
                {
                    DiscordMember? m = await this.HomeGuild.GetMemberAsync(ownerId);

                    int rank = 0;
                    foreach (DiscordRole? role in m.Roles)
                    {
                        PartnerBotDonorRoleConfiguration? r;
                        if ((r = this._pcfg.DonorRoles.FirstOrDefault(x => x.RoleId == role.Id)) is not null)
                        {
                            rank += r.Weight;
                        }
                    }

                    return rank;
                }
                catch (Exception ex)
                {
                    this._rest.Logger.LogWarning(ex, $"Donor rank get failed for {ownerId}");
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
    }
}
