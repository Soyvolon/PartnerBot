using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using PartnerBot.Discord.Commands;
using PartnerBot.Discord.Commands.Conditions;
using PartnerBot.Discord.Slash.Conditions;

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

        public override async Task<bool> BeforeContextMenuExecutionAsync(ContextMenuContext ctx)
        {
            Ctx = ctx;
            // for now
            var res = await new RequireServerAdminOrOwnerSlashAttribute().ExecuteChecksAsync(ctx);
            if (!res) throw new Exception("Checks failed");
            return await base.BeforeContextMenuExecutionAsync(ctx);
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
