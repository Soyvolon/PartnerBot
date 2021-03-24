﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;

using PartnerBot.Core.Entities;
using PartnerBot.Core.Entities.Configuration;

namespace PartnerBot.Core.Services
{
    public class DonorService
    {
        public static readonly int MAX_EMBEDS = 4;
        private readonly DiscordShardedClient _client;
        private readonly PartnerBotConfiguration _pcfg;

        private DiscordGuild? HomeGuild = null;

        public DonorService(DiscordShardedClient client, PartnerBotConfiguration pcfg)
        {
            _client = client;
            _pcfg = pcfg;
        }

        public async Task<int> GetDonorRank(Partner partner)
        {
            if(HomeGuild is null)
            {
                foreach (var shard in _client.ShardClients.Values)
                    if (shard.Guilds.TryGetValue(_pcfg.HomeGuild, out HomeGuild))
                        break;
            }

            if (HomeGuild is not null)
            {
                var m = await HomeGuild.GetMemberAsync(partner.OwnerId);

                int rank = 0;
                foreach (var role in m.Roles)
                {
                    PartnerBotDonorRoleConfiguration? r;
                    if ((r = _pcfg.DonorRoles.FirstOrDefault(x => x.RoleId == role.Id)) is not null)
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
