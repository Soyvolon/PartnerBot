using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using PartnerBot.Core.Services;
using PartnerBot.Discord.Commands.Conditions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartnerBot.Discord.Commands.Admin
{
    public class ForceNSFWCommand : CommandModule
    {
        private readonly PartnerManagerService _manager;

        public ForceNSFWCommand(PartnerManagerService manager)
        {
            _manager = manager;
        }

        [Command("forcensfw")]
        [RequireCessumStaff]
        [Description("Force a guild to be marked as NSFW")]
        public async Task ForceNSFWCommandAsync(CommandContext ctx, 
            [Description("The guild to force NSFW for.")]
            ulong guildId)
        {
            var res = await _manager.UpdateOrAddPartnerAsync(guildId, () => new()
            {
                NSFW = true
            });

            if (res.Item1 is null)
            {
                await RespondError(res.Item2);
            }
            else
            {
                await RespondSuccess($"Forced the guild {res.Item1.GuildName} [{res.Item1.GuildId}] to be marked NSFW.");
            }
        }
    }
}
