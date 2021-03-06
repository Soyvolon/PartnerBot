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
        [Description("Interactive Setup for Partner Bot")]
        [RequireServerAdminOrOwner]
        public async Task InteractiveSetupCommandAsync(CommandContext ctx)
        {

        }

        private async Task 
    }
}
