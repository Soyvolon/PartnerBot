using System;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using Microsoft.Extensions.DependencyInjection;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;

namespace PartnerBot.Discord.Commands.Conditions
{
    /// <summary>
    /// [Currently Broken] Marks this as a command that requiers pb!setup to have already been completed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    class RequirePartnerSetupAttribute : CheckBaseAttribute
    {
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (DiscordBot.Services is null) return false;

            PartnerDatabaseContext? db = DiscordBot.Services.GetRequiredService<PartnerDatabaseContext>();
            Partner? partner = await db.FindAsync<Partner>(ctx.Guild.Id);

            return partner is not null && partner.IsSetup();
        }
    }
}
