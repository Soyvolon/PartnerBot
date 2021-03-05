using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace PartnerBot.Discord.Commands
{
    public class CommandModule : BaseCommandModule
    {
        private CommandContext ctx;

        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            this.ctx = ctx;
            base.BeforeExecutionAsync(ctx);
            return Task.CompletedTask;
        }

        protected async Task RespondError(string message)
        {
            await ctx.RespondAsync(ErrorBase().WithDescription(message));
        }

        protected async Task RespondSuccess(string message)
        {
            await ctx.RespondAsync(SuccessBase().WithDescription(message));
        }

        public static DiscordEmbedBuilder ErrorBase()
        {
            return new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red);
        }

        public static DiscordEmbedBuilder SuccessBase()
        {
            return new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Green);
        }
    }
}
