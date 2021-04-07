using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using Microsoft.Extensions.DependencyInjection;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;
using PartnerBot.Core.Entities.Configuration;

namespace PartnerBot.Discord.Commands.Utility
{
    public class PrefixCommand : CommandModule
    {
        private readonly IServiceProvider _services;

        public PrefixCommand(IServiceProvider services)
        {
            _services = services;
        }

        [Command("prefix")]
        [Description("Set the prefix for your server.")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task PrefixCommandAsync(CommandContext ctx, 
            [Description("The new prefix for your server.")]
            string newPrefix)
        {
            if(string.IsNullOrWhiteSpace(newPrefix))
            {
                await RespondError("The new prefix cannon't be null or white space.");
                return;
            }
            
            var db = _services.GetRequiredService<PartnerDatabaseContext>();
            var config = await db.FindAsync<DiscordGuildConfiguration>(ctx.Guild.Id);

            config.Prefix = newPrefix;

            await db.SaveChangesAsync();

            await RespondSuccess($"Your servers prefix is now `{newPrefix}`");
        }
    }
}
