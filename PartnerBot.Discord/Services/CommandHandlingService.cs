using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.EventArgs;

namespace PartnerBot.Discord.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordShardedClient _client;

        public CommandHandlingService(DiscordShardedClient client)
        {
            _client = client;
        }

        public Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {

        }
    }
}
