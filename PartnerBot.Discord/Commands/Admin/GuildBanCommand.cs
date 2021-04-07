using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using PartnerBot.Core.Services;
using PartnerBot.Discord.Commands.Conditions;

namespace PartnerBot.Discord.Commands.Admin
{
    public class GuildBanCommand : BaseCommandModule
    {
        private readonly GuildBanService _ban;

        public GuildBanCommand(GuildBanService ban)
        {
            _ban = ban;
        }

        [Command("ban")]
        [Description("Ban a guild from the bot.")]
        [RequireCessumAdmin]
        public async Task GuildBanCommandAsync(CommandContext ctx, 
            [Description("Guild ID to ban.")]
            ulong guildId, 

            [Description("Reason for the ban.")]
            [RemainingText] 
            string? reason = null)
        {
            var ban = await _ban.BanGuildAsync(guildId, reason);

            await ctx.RespondAsync($"Guild {ban.GuildId} banned succesfully.");

            await _ban.FinalizeBanAsync(ban.GuildId, ban.Reason);

            await ctx.RespondAsync($"Partner Bot Disconnected from guild {ban.GuildId}.");
        }

        [Command("unban")]
        [Description("Unban a guild from the bot")]
        [RequireCessumAdmin]
        public async Task GuildUnbanCommandAsync(CommandContext ctx,
            [Description("Guild ID to unban")]
            ulong guildId)
        {

        }
    }
}
