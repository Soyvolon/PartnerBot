using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace PartnerBot.Discord.Services
{
    public class CommandErrorHandlingService
    {
        public async Task RespondCommandNotFound(DiscordChannel c, string prefix)
        {
            await c.SendMessageAsync(new DiscordEmbedBuilder()
                .WithDescription($"Command Not Found. Use `{prefix}help` to see a list of commands. Or, take a look at our [Documentation](https://soyvolon.github.io/PartnerBot) for more" +
                $" information!")
                .WithColor(DiscordColor.DarkRed));
        }

        public async Task Client_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            sender.Client.Logger.LogWarning(DiscordBot.Event_ClientLogger, $"Command {e.Command.Name} errored when executed by {e.Context.User.Username} on {e.Context.Guild.Name}");
            sender.Client.Logger.LogDebug(DiscordBot.Event_ClientLogger, e.Exception, $"Exception Information for Command Error");

#if DEBUG
            await e.Context.RespondAsync($"The following error occoured: {e.Exception.Message} at ```{e.Exception.StackTrace}```");
#else
            await e.Context.RespondAsync($"The following error occoured: {e.Exception.Message}");
#endif
        }

        public Task Client_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            sender.Client.Logger.LogDebug(DiscordBot.Event_ClientLogger, $"Command {e.Command.Name} executed by {e.Context.User.Username} on {e.Context?.Guild?.Name ?? ""}");
            return Task.CompletedTask;
        }
    }
}
