using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using PartnerBot.Discord.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartnerBot.Discord.Slash
{
    public class SlashCommandBase : ApplicationCommandModule
    {
        protected BaseContext Ctx { get; set; }

        public override Task<bool> BeforeContextMenuExecutionAsync(ContextMenuContext ctx)
        {
            Ctx = ctx;
            return base.BeforeContextMenuExecutionAsync(ctx);
        }

        public override Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
        {
            Ctx = ctx;
            return base.BeforeSlashExecutionAsync(ctx);
        }

        protected async Task RespondError(string message)
        {
            await Ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(CommandModule.ErrorBase()
                        .WithDescription(message)));
        }

        protected async Task RespondSuccess(string message)
        {
            await Ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(CommandModule.SuccessBase()
                        .WithDescription(message)));
        }
    }
}
