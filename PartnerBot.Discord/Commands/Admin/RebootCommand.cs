using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using PartnerBot.Discord.Commands.Conditions;
using System.Threading.Tasks;

namespace PartnerBot.Discord.Commands.Admin
{
    public class RebootCommand : CommandModule
    {
        private readonly DiscordShardedClient _client;
        public RebootCommand(DiscordShardedClient client)
        {
            _client = client;
        }

        [Command("reboot")]
        [Description("Reboots the selected shard.")]
        [RequireCessumAdmin]
        public async Task RebootCommandAsync(CommandContext ctx, [Description("Shard to reboot")] int shard)
        {
            if(_client.ShardClients.TryGetValue(shard, out var client))
            {
                await client.DisconnectAsync();
                await client.ConnectAsync();
                await RespondSuccess($"Shard {shard} restarted.");
            }
            else
            {
                await RespondError($"Unable to get a shard by the ID of {shard}");
            }
        }
    }
}
