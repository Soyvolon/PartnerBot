using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using PartnerBot.Core.Entities.Configuration;

namespace PartnerBot.Discord.Commands.Utility
{
    public class InviteCommand : CommandModule
    {
        private readonly DiscordShardedClient _client;
        private readonly PartnerBotConfiguration _config;

        private string? invite { get; set; } = null;

        public InviteCommand(PartnerBotConfiguration config, DiscordShardedClient client)
        {
            _client = client;
            _config = config;
        }

        [Command("invite")]
        [Description("Invite Partner Bot!")]
        public async Task InviteCommandAsync(CommandContext ctx)
        {
            if (invite is null)
            {
                invite = $"https://discord.com/api/oauth2/authorize?client_id={_client.CurrentApplication.Id}&permissions={(uint)_config.BotPermissions}&scope=bot%20applications.commands";
                invite = $"Invite Partner Bot [here]({invite})";
            }

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithDescription(invite)
                .WithColor(Color_PartnerBotMagenta));
        }
    }
}
