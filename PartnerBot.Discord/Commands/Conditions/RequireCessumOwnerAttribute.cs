using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using PartnerBot.Core.Entities.Configuration;

namespace PartnerBot.Discord.Commands.Conditions
{
    /// <summary>
    /// Marks this a Cessum owner exclusive command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireCessumOwnerAttribute : CheckBaseAttribute
    {
        protected readonly PartnerBotConfiguration _pcfg;

        public RequireCessumOwnerAttribute()
        {
            this._pcfg = DiscordBot.PbCfg ?? new("", "pb!", new(), new(), 0, new(), Permissions.None, "");
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (isOwner(ctx.Member.Id)) return Task.FromResult(true);

            return Task.FromResult(false);
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
