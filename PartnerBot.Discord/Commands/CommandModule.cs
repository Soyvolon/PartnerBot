using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace PartnerBot.Discord.Commands
{
    public class CommandModule : BaseCommandModule
    {
        public static readonly DiscordColor Color_PartnerBotMagenta = new(0xe91e63);

        protected CommandContext Context { get; private set; }

        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            this.Context = ctx;
            base.BeforeExecutionAsync(ctx);
            return Task.CompletedTask;
        }

        protected async Task RespondError(string message)
        {
            await Context.RespondAsync(ErrorBase().WithDescription(message));
        }

        protected async Task RespondSuccess(string message)
        {
            await Context.RespondAsync(SuccessBase().WithDescription(message));
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

        public async Task InteractTimeout(string message = "Interactivty Timed Out.")
        {
            var embed = ErrorBase()
                .WithDescription(message);

            await Context.RespondAsync(embed: embed);
        }
    }
}
