﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;

using PartnerBot.Core.Services;

namespace PartnerBot.Discord.Commands.Testing
{
    [Group("test")]
    [RequireOwner]
    public partial class TestCommandGroup : CommandModule
    {
        private readonly PartnerSenderService _sender;
        private readonly DiscordShardedClient _client;

        public TestCommandGroup(PartnerSenderService sender, DiscordShardedClient client)
        {
            _sender = sender;
            _client = client;
        }
    }
}