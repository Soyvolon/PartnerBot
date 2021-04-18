using System.Collections.Generic;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using PartnerBot.Core.Entities;
using PartnerBot.Core.Entities.Moderation;
using PartnerBot.Core.Services;
using PartnerBot.Discord.Commands.Conditions;

namespace PartnerBot.Discord.Commands.Core
{
    public partial class SetupCommand : CoreCommandModule
    {
        [Command("setup")]
        [Hidden]
        [RequireServerAdminOrOwner]
        [Description("Basic setup command.")]
        public async Task BasicSetupCommand(CommandContext ctx, 
            [Description("Partner channel.")]
            DiscordChannel channel, 

            [RemainingText]
            [Description("Partner message.")]
            string message)
        {
            GuildBan? ban;
            if ((ban = await this._ban.GetBanAsync(ctx.Guild.Id)) is not null)
            {
                await RespondError($"Your server is banned due to: {ban.Reason}\n\n" +
                    $"Contact a staff member on the [support server](https://discord.gg/3SCTnhCMam) to learn more.");

                await ctx.Guild.LeaveAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                await RespondError("The partner message can not be null or white space!");
                return;
            }

            if(GuildVerificationService.VerifyChannel(channel))
            {
                int drank = await this._donor.GetDonorRankAsync(ctx.Guild.OwnerId);
                (Partner?, string) res = await this._partners.UpdateOrAddPartnerAsync(ctx.Guild.Id, () => {
                    PartnerUpdater dat = new()
                    {
                        Active = true,
                        ChannelId = channel.Id,
                        Message = message,
                        GuildIcon = ctx.Guild.IconUrl,
                        GuildName = ctx.Guild.Name,
                        UserCount = ctx.Guild.MemberCount,
                        OwnerId = ctx.Guild.OwnerId,
                        DonorRank = drank
                    };

                    return dat;
                });

                if (res.Item1 is not null)
                {
                    await RespondSuccess($"Partner Bot is now setup and toggled on! A test message has been sent to {channel.Mention}");

                    PartnerData? data = res.Item1.BuildData(res.Item1, null);

                    await data.ExecuteAsync(this._rest, new());
                }
                else
                {
                    await RespondError($"Partner Bot failed to save: {res.Item2}");
                }
            }
            else
            {
                (List<string>, List<DiscordOverwrite>) invalidRes = await GetInvalidChannelSetupDataString(channel);

                await RespondError($"**Invalid Channel Setup.**\n" +
                            $"Some overwrites are missing the `View Channel` or `Read Message History` for {channel.Mention}\n\n" +
                            $"{string.Join("\n\n", invalidRes.Item1)}");
            }
        }
    }
}
