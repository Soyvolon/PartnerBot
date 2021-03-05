using System;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace PartnerBot.Discord.Services
{
    public class CommandErrorHandlingService
    {
        public async Task RespondCommandNotFound(DiscordChannel c, string prefix)
        {
            throw new NotImplementedException();
        }

        public Task Client_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e) => throw new NotImplementedException();

        public Task Client_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e) => throw new NotImplementedException();
    }
}
