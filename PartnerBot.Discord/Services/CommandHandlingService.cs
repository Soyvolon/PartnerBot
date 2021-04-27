using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities.Configuration;

namespace PartnerBot.Discord.Services
{
    public class CommandHandlingService
    {
        private readonly ILogger _logger;
        private readonly CommandErrorHandlingService _error;
        private readonly PartnerBotConfiguration _pcfg;
        private readonly IServiceProvider _services;

        public CommandHandlingService(DiscordShardedClient client, CommandErrorHandlingService error,
            PartnerBotConfiguration pcfg, IServiceProvider services)
        {
            this._logger = client.Logger;
            this._error = error;
            this._pcfg = pcfg;
            this._services = services;
        }

        public Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            this._logger.LogTrace(DiscordBot.Event_CommandHandler, $"Message Created: {e}");
            
            if (e.Author.IsBot) return Task.CompletedTask;

            if(e.Guild is not null)
            {
                _ = Task.Run(async () => await ExecuteCommandAsync(sender, e));
            }
            else
            {
                _ = Task.Run(async () => await ExecuteDMCommandAsync(sender, e));
            }

            return Task.CompletedTask;
        }

        private async Task ExecuteDMCommandAsync(DiscordClient sender, MessageCreateEventArgs e)
        {
            string mention = sender.CurrentUser.Mention;
            if (!e.Message.Content.StartsWith(mention))
            {
                string? nick = mention.Insert(2, "!");
                if (e.Message.Content.StartsWith(nick))
                    mention = nick;
                else
                    return;
            }

            string? prefix = e.Message.Content.Substring(0, mention.Length);
            string commandString = e.Message.Content.Substring(mention.Length);

            CommandsNextExtension? cnext = sender.GetCommandsNext();

            Command? command = cnext.FindCommand(commandString, out string args);

            if (command is null)
            { 
                // Looks like that command does not exsist.
                await this._error.RespondCommandNotFound(e.Channel, prefix);
            }
            else
            {
                // We found a command, lets deal with it.
                CommandContext? ctx = cnext.CreateContext(e.Message, prefix, command, args);
                // We are done here, its up to CommandsNext now.

                await cnext.ExecuteCommandAsync(ctx);
            }
        }

        private async Task ExecuteCommandAsync(DiscordClient sender, MessageCreateEventArgs e)
        {
            using var scope = this._services.CreateScope();
            var model = scope.ServiceProvider.GetRequiredService<PartnerDatabaseContext>();

            DiscordGuildConfiguration guildConfig = await model.FindAsync<DiscordGuildConfiguration>(e.Guild.Id);

            if (guildConfig is null)
            {
                guildConfig = new DiscordGuildConfiguration
                {
                    GuildId = e.Guild.Id,
                    Prefix = this._pcfg.Prefix
                };

                model.Add(guildConfig);

                await model.SaveChangesAsync();
            }

            int prefixPos = await PrefixResolver(sender, e.Message, guildConfig);

            if (prefixPos == -1)
                // Prefix is wrong, dont respond to this message.
                return;

            string? prefix = e.Message.Content.Substring(0, prefixPos);
            string commandString = e.Message.Content.Substring(prefixPos);

            CommandsNextExtension? cnext = sender.GetCommandsNext();

            Command? command = cnext.FindCommand(commandString, out string args);

            if (command is null)
            { 
                // Looks like that command does not exsist.
                await this._error.RespondCommandNotFound(e.Channel, prefix);
            }
            else
            {
                // We found a command, lets deal with it.
                CommandContext? ctx = cnext.CreateContext(e.Message, prefix, command, args);
                // We are done here, its up to CommandsNext now.

                await cnext.ExecuteCommandAsync(ctx);
            }
        }

        private async Task<int> PrefixResolver(DiscordClient _client, DiscordMessage msg, DiscordGuildConfiguration guildConfig)
        {
            //Checks if bot can't send messages, if so ignore.
            if (!msg.Channel.PermissionsFor(await msg.Channel.Guild.GetMemberAsync(_client.CurrentUser.Id)).HasPermission(Permissions.SendMessages)) return -1;
            // Always respond to a mention.
            else if (msg.Content.StartsWith(_client.CurrentUser.Mention)) return _client.CurrentUser.Mention.Length;
            else
            {
                try
                {
                    if (guildConfig.Prefix is null)
                    {
                        guildConfig.Prefix = this._pcfg.Prefix;
                    }

                    if (msg.Content.StartsWith(guildConfig.Prefix))
                        //Return length of server prefix.
                        return guildConfig?.Prefix?.Length ?? -1; 
                    else
                        return -1;
                }
                catch (Exception err)
                {
                    this._logger.LogError(DiscordBot.Event_CommandHandler, err, $"Prefix Resolver failed in guild {msg.Channel.Guild.Name}:");
                    return -1;
                }
            }
        }
    }
}
