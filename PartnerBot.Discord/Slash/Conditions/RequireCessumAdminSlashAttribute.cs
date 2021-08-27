﻿using System;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace PartnerBot.Discord.Slash.Conditions
{
    /// <summary>
    /// Marks this a Cessum admin exclusive command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireCessumAdminSlashAttribute : RequireCessumOwnerSlashAttribute
    {
        public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
        {
            if (isOwner(ctx.Member.Id)) return true;

            (bool, DiscordMember?) adminResult = await IsCessumAdmin(ctx);

            if (adminResult.Item1) return true;

            return false;
        }

        public async Task<(bool, DiscordMember?)> IsCessumAdmin(BaseContext ctx)
        {
            if (DiscordBot.Client is null) return (false, null);

            DiscordMember? staffMember = null;

            try
            {
                foreach (DiscordClient? c in DiscordBot.Client.ShardClients.Values)
                {
                    if (c.Guilds.TryGetValue(this._pcfg.HomeGuild, out DiscordGuild? g))
                    {
                        staffMember = await g.GetMemberAsync(ctx.Member.Id);
                    }
                }
            }
            catch
            {
                return (false, null);
            }

            if (staffMember == null) return (false, null);

            if (staffMember.Roles.Any(r => r.Permissions.HasPermission(Permissions.Administrator))) return (true, staffMember);

            return (false, staffMember);
        }
    }
}
