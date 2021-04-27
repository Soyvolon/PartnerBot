using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using Microsoft.Extensions.DependencyInjection;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;
using PartnerBot.Core.Services;
using PartnerBot.Discord.Commands.Conditions;

namespace PartnerBot.Discord.Commands.Admin
{
    public class RunLinkRemovalCommand : CommandModule
    {
        private readonly IServiceProvider _services;

        public RunLinkRemovalCommand(IServiceProvider services)
        {
            this._services = services;
        }

        [Command("removelinks")]
        [Description("Removes links from a partner message and returns the new message.")]
        [Hidden]
        [RequireCessumStaff]
        public async Task RunLinkRemovalCommandAsync(CommandContext ctx, ulong guildId)
        {
            using var scope = this._services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PartnerDatabaseContext>();
            var partner = await db.FindAsync<Partner>(guildId);

            if(partner is null)
            {
                await RespondError("No partner found.");
                return;
            }

            partner.ModifyToDonorRank();

            db.Update(partner);
            await db.SaveChangesAsync();

            await RespondSuccess($"Modified to donor rank: {partner.DonorRank}");
            await ctx.RespondAsync($"```{partner.Message}```");
        }
    }
}
