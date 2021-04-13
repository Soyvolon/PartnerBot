using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;

using Microsoft.Extensions.DependencyInjection;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities.Configuration;
using PartnerBot.Core.Services;
using PartnerBot.Discord.Commands.Conditions;

namespace PartnerBot.Discord.Commands.Utility
{
    public class PurgeCommand : CommandModule
    {
        private readonly PartnerManagerService _partners;
        private readonly IServiceProvider _services;

        public PurgeCommand(PartnerManagerService partners, IServiceProvider services)
        {
            _partners = partners;
            _services = services;
        }

        [Command("purge")]
        [Description("Removes all user data from your server, then removes the bot.")]
        [RequireServerAdminOrOwner]
        public async Task PurgeCommandAsync(CommandContext ctx)
        {
            var interact = ctx.Client.GetInteractivity();

            await ctx.RespondAsync("**Purging your data will delete all saved partner information then remove the bot from your server. Are you sure" +
                " you want to proocede? Type `yes` to purge data. Type anything else to cancle this operation.**");

            var res = await interact.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id);

            if(res.TimedOut)
            {
                await RespondError("Time out.");
            }
            else
            {
                if(res.Result.Content.Trim().ToLower().Equals("yes"))
                {                    
                    await _partners.RemovePartnerAsync(ctx.Guild.Id);

                    var db = _services.GetRequiredService<PartnerDatabaseContext>();

                    await RespondSuccess("Partner config purged, leaving guild.\n\n" +
                        "Thanks for using Partner Bot!");

                    var guild = await db.FindAsync<DiscordGuildConfiguration>(ctx.Guild.Id);
                    db.Remove(guild);

                    await db.SaveChangesAsync();
                    
                    await ctx.Guild.LeaveAsync();
                }
                else
                {
                    await RespondError("Aborting...");
                }
            }
        }
    }
}
