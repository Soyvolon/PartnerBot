using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using PartnerBot.Core.Services;
using PartnerBot.Discord.Slash.Conditions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartnerBot.Discord.Slash.Admin
{
    public class BanCommand : SlashCommandBase
    {
        private readonly GuildBanService _ban;

        public BanCommand(GuildBanService ban)
        {
            this._ban = ban;
        }

        [SlashCommand("ban", "Ban a guild or owner from Partner Bot")]
        [RequireCessumStaffSlash]
        public async Task BanCommandAsync(InteractionContext ctx,
            [Option("Id", "The guild or user to ban.")]
            string id,
            
            [Option("Reason", "Reason for the ban")]
            string reason)
        {
            if(ulong.TryParse(id, out var res))
            {
                PartnerBot.Core.Entities.Moderation.GuildBan? ban = await this._ban.BanGuildAsync(res, reason);

                await this._ban.FinalizeBanAsync(ban.GuildId, ban.Reason);

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Guild {ban.GuildId} banned successfully."));
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("A valid ID was not provided."));
        }
    }
}
