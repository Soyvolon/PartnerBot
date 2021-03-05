using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;

namespace PartnerBot.Discord.Services
{
    public class CommandErrorHandlingService
    {
        public Task Client_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e) => throw new NotImplementedException();

        public Task Client_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e) => throw new NotImplementedException();
    }
}
