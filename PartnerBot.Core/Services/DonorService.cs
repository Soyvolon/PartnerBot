using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;

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

        private DiscordGuild? HomeGuild = null;

        public DonorService(DiscordShardedClient client, PartnerBotConfiguration pcfg)
        {
            this._client = client;
            this._pcfg = pcfg;
        }

        public async Task<int> GetDonorRankAsync(ulong ownerId)
        {
            if(this.HomeGuild is null)
            {
                foreach (DiscordClient? shard in this._client.ShardClients.Values)
                    if (shard.Guilds.TryGetValue(this._pcfg.HomeGuild, out this.HomeGuild))
                        break;
            }

            if (this.HomeGuild is not null)
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
            else
            {
                return 0;
            }
        }
    }
}
