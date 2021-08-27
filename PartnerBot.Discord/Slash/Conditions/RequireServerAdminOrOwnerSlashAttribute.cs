using System;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;

namespace PartnerBot.Discord.Slash.Conditions
{
    /// <summary>
    /// Marks this a Cessum owner or server admin exclusive command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequireServerAdminOrOwnerSlashAttribute : RequireCessumOwnerSlashAttribute
    {
        public override Task<bool> ExecuteChecksAsync(BaseContext ctx)
        {
            if (isOwner(ctx.Member.Id)) return Task.FromResult(true);

            if (ctx.Member.IsOwner) return Task.FromResult(true);

            if (ctx.Member.Roles.Any(r => r.Permissions.HasPermission(Permissions.Administrator))) return Task.FromResult(true);

            return Task.FromResult(false);
        }
    }
}
