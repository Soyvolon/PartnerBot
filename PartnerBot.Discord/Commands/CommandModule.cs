using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace PartnerBot.Discord.Commands
{
    public class CommandModule : BaseCommandModule
    {
        public static readonly DiscordColor Color_PartnerBotMagenta = new(0xe91e63);

        protected CommandContext Context { get; private set; }
        protected DiscordEmoji Lock { get; private set; }
        protected DiscordEmoji Check { get; private set; }
        protected DiscordEmoji Cross { get; private set; }

        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            this.Context = ctx;
            base.BeforeExecutionAsync(ctx);

            this.Lock = DiscordEmoji.FromName(ctx.Client, ":lock:");
            this.Check = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            this.Cross = DiscordEmoji.FromName(ctx.Client, ":x:");

            return Task.CompletedTask;
        }

        protected async Task RespondError(string message)
        {
            await this.Context.RespondAsync(ErrorBase().WithDescription(message));
        }

        protected async Task RespondSuccess(string message)
        {
            await this.Context.RespondAsync(SuccessBase().WithDescription(message));
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

        public async Task InteractTimeout(string message = "Interactivity Timed Out.")
        {
            DiscordEmbedBuilder? embed = ErrorBase()
                .WithDescription(message);

            await this.Context.RespondAsync(embed: embed);
        }
    }
}
