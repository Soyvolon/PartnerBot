using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using PartnerBot.Core.Services;

namespace PartnerBot.Discord.Commands.Utility
{
    public class ViewMessageCommand : CommandModule
    {
        private readonly PartnerManagerService _partners;

        public ViewMessageCommand(PartnerManagerService partners)
        {
            this._partners = partners;
        }

        [Command("message")]
        [Description("Allows you to view your currently set message.")]
        [Aliases("msg")]
        public async Task ViewMessageCommandAsync(CommandContext ctx)
        {
            var msg = await _partners.GetPartnerElementAsync(ctx.Guild.Id, x => x.Message);

            if(string.IsNullOrWhiteSpace(msg))
            {
                await RespondError("No message has been set on this server!");
            }
            else
            {
                await ctx.RespondAsync(msg);
                await ctx.RespondAsync($"```{msg}```");
            }
        }
    }
}
