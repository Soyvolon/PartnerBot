using System;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace PartnerBot.Discord.Commands.Conditions
{
    /// <summary>
    /// Marks this a Cessum staff exclusive command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireCessumStaffAttribute : RequireCessumAdminAttribute
    {
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
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
