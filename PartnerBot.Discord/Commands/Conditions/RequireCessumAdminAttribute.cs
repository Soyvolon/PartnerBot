using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

using Newtonsoft.Json.Linq;

using PartnerBot.Core.Entities.Configuration;

namespace PartnerBot.Discord.Commands.Conditions
{
    /// <summary>
    /// Marks this a Cessum admin exclusive command
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireCessumAdminAttribute : RequireCessumOwnerAttribute
    {
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (isOwner(ctx.Member.Id)) return true;

            var adminResult = await IsCessumAdmin(ctx);

            if (adminResult.Item1) return true;

            return false;
        }

        public async Task<(bool, DiscordMember?)> IsCessumAdmin(CommandContext ctx)
        {
            if (DiscordBot.Client is null) return (false, null);

            DiscordMember? staffMember = null;

            try
            {
                foreach (var c in DiscordBot.Client.ShardClients.Values)
                {
                    if (c.Guilds.TryGetValue(_pcfg.HomeGuild, out var g))
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
