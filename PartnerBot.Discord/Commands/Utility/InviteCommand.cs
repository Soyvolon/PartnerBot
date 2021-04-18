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
            this._client = client;
            this._config = config;
        }

        [Command("invite")]
        [Description("Invite Partner Bot!")]
        public async Task InviteCommandAsync(CommandContext ctx)
        {
            if (this.invite is null)
            {
                this.invite = $"https://discord.com/api/oauth2/authorize?client_id={this._client.CurrentApplication.Id}&permissions={(uint)this._config.BotPermissions}&scope=bot%20applications.commands";
                this.invite = $"Invite Partner Bot [here]({this.invite})";
            }

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithDescription(this.invite)
                .WithColor(Color_PartnerBotMagenta));
        }
    }
}
