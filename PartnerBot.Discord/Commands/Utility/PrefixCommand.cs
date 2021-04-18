using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using Microsoft.Extensions.DependencyInjection;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities.Configuration;

namespace PartnerBot.Discord.Commands.Utility
{
    public class PrefixCommand : CommandModule
    {
        private readonly IServiceProvider _services;

        public PrefixCommand(IServiceProvider services)
        {
            this._services = services;
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
                await RespondError("The new prefix cannot be null or white space.");
                return;
            }

            PartnerDatabaseContext? db = this._services.GetRequiredService<PartnerDatabaseContext>();
            DiscordGuildConfiguration? config = await db.FindAsync<DiscordGuildConfiguration>(ctx.Guild.Id);

            config.Prefix = newPrefix;

            await db.SaveChangesAsync();

            await RespondSuccess($"Your servers prefix is now `{newPrefix}`");
        }
    }
}
