using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.SlashCommands;

using PartnerBot.Core.Entities.Configuration;

namespace PartnerBot.Discord.Slash.Conditions
{
    /// <summary>
    /// Marks this a Cessum owner exclusive command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireCessumOwnerSlashAttribute : SlashCheckBaseAttribute
    {
        protected readonly PartnerBotConfiguration _pcfg;

        public RequireCessumOwnerSlashAttribute()
        {
            this._pcfg = DiscordBot.PbCfg ?? new("", "pb!", new(), new(), 0, new(), Permissions.None, "", 1);
        }

        public virtual Task<bool> ExecuteChecksAsync(BaseContext ctx)
        {
            if (isOwner(ctx.Member.Id)) return Task.FromResult(true);

            return Task.FromResult(false);
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            return await ExecuteChecksAsync(ctx);
        }

        // Determines if an ID is an owner.
        public bool isOwner(ulong id) 
        {
            // Loop through the owner ID array.
            foreach (ulong ID in this._pcfg.Owners)
            {
                if (ID == id)
                    return true;
            }
            return false;
        }
    }
}
