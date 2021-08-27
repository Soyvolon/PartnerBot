using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartnerBot.Discord.Slash.Conditions
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireCessumStaffSlashAttribute : RequireCessumAdminSlashAttribute
    {
        public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
        {
            if (isOwner(ctx.Member.Id)) return true;

            (bool, DiscordMember?) adminResult = await IsCessumAdmin(ctx);

            if (adminResult.Item1) return true;

            if (adminResult.Item2 != null)
            {
                DiscordMember member = adminResult.Item2;
                foreach (DiscordRole role in member.Roles)
                    if (this._pcfg.StaffRoles.Contains(role.Id))
                        return true;
            }
            
            return false;
        }
    }
}
