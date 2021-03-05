using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using PartnerBot.Core.Entities.Configuration;
using PartnerBot.Discord.Commands.Conditions;

namespace PartnerBot.Discord.Commands.Core
{
    public class SetupCommand : CommandModule
    {
        [Command("setup")]
        [Description("First time setup for Partner Bot. Can be used multiple times.")]
        [RequireServerAdminOrOwner]
        public async Task SetupCommandAsync(CommandContext ctx, 
            [Description("Channel to send partner messages to.")]
            DiscordChannel channel, 

            [RemainingText]
            [Description("Partner message to send to other servers.")]
            string message = "")
        {

        }

        [Command("setup")]
        [Description("Interactive Setup for Partner Bot")]
        public async Task InteractiveSetupCommandAsync(CommandContext ctx)
        {

        }
    }
}
