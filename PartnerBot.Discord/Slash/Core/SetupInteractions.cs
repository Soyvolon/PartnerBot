using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using PartnerBot.Core.Entities;
using PartnerBot.Core.Entities.Moderation;
using PartnerBot.Core.Services;
using PartnerBot.Core.Utils;
using PartnerBot.Discord.Commands.Core;
using PartnerBot.Discord.Slash.Conditions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PartnerBot.Discord.Slash.Core
{
    public class SetupInteractions : SlashCommandBase
    {

        private readonly IServiceProvider _services;
        private readonly DonorService _donor;
        private readonly PartnerManagerService _partners;
        private readonly GuildBanService _ban;
        private readonly DiscordRestClient _rest;

        public SetupInteractions(IServiceProvider services, DonorService donor,
           PartnerManagerService partners, GuildBanService ban,
           DiscordRestClient rest)
        {
            this._services = services;
            this._donor = donor;
            this._partners = partners;
            this._ban = ban;
            this._rest = rest;
        }

        [ContextMenu(ApplicationCommandType.MessageContextMenu, "Set Partner Message")]
        [RequireServerAdminOrOwnerSlash]
        public async Task SetMessageContextMenuAsync(ContextMenuContext ctx)
        {
            string message = ctx.TargetMessage.Content;
            GuildBan? ban;
            if ((ban = await this._ban.GetBanAsync(ctx.Guild.Id)) is not null
                || (ban = await this._ban.GetBanAsync(ctx.Guild.OwnerId)) is not null)
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

            IReadOnlyList<string>? links = message.GetUrls();

            foreach (string? l in links)
            {
                message = message.Remove(message.IndexOf(l), l.Length);
            }

            int drank = await this._donor.GetDonorRankAsync(ctx.Guild.OwnerId);
            (Partner?, string) res = await this._partners.UpdateOrAddPartnerAsync(ctx.Guild.Id, () => {
                PartnerUpdater dat = new()
                {
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
                await RespondSuccess($"Partner Bot message has now been updated to:");
                await Ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(message));
            }
            else
            {
                await RespondError($"Partner Bot failed to save: {res.Item2}");
            }
        }

        [ContextMenu(ApplicationCommandType.MessageContextMenu, "Set Partner Channel")]
        [RequireServerAdminOrOwnerSlash]
        public async Task SetChannelContextMenuAsync(ContextMenuContext ctx)
        {
            var channel = ctx.Channel;
            GuildBan? ban;
            if ((ban = await this._ban.GetBanAsync(ctx.Guild.Id)) is not null
                || (ban = await this._ban.GetBanAsync(ctx.Guild.OwnerId)) is not null)
            {
                await RespondError($"Your server is banned due to: {ban.Reason}\n\n" +
                    $"Contact a staff member on the [support server](https://discord.gg/3SCTnhCMam) to learn more.");

                await ctx.Guild.LeaveAsync();
                return;
            }

            if (GuildVerificationService.VerifyChannel(channel))
            {
                int drank = await this._donor.GetDonorRankAsync(ctx.Guild.OwnerId);
                (Partner?, string) res = await this._partners.UpdateOrAddPartnerAsync(ctx.Guild.Id, () => {
                    PartnerUpdater dat = new()
                    {
                        ChannelId = channel.Id,
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
                    await RespondSuccess($"Your partner channel has been set to {channel.Mention}");
                }
                else
                {
                    await RespondError($"Partner Bot failed to save: {res.Item2}");
                }
            }
            else
            {
                (List<string>, List<DiscordOverwrite>) invalidRes = await CoreCommandModule.GetInvalidChannelSetupDataString(channel);

                await RespondError($"**Invalid Channel Setup.**\n" +
                            $"Some overwrites are missing the `View Channel` or `Read Message History` for {channel.Mention}\n\n" +
                            $"{string.Join("\n\n", invalidRes.Item1)}");
            }
        }

        [ContextMenu(ApplicationCommandType.MessageContextMenu, "Basic Setup")]
        [RequireServerAdminOrOwnerSlash]
        public async Task SetMessageChannelContextMenuAsync(ContextMenuContext ctx)
        {
            string message = ctx.TargetMessage.Content;
            var channel = ctx.Channel;
            GuildBan? ban;
            if ((ban = await this._ban.GetBanAsync(ctx.Guild.Id)) is not null
                || (ban = await this._ban.GetBanAsync(ctx.Guild.OwnerId)) is not null)
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

            if (GuildVerificationService.VerifyChannel(channel))
            {
                IReadOnlyList<string>? links = message.GetUrls();

                foreach (string? l in links)
                {
                    message = message.Remove(message.IndexOf(l), l.Length);
                }

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
                (List<string>, List<DiscordOverwrite>) invalidRes = await CoreCommandModule.GetInvalidChannelSetupDataString(channel);

                await RespondError($"**Invalid Channel Setup.**\n" +
                            $"Some overwrites are missing the `View Channel` or `Read Message History` for {channel.Mention}\n\n" +
                            $"{string.Join("\n\n", invalidRes.Item1)}");
            }
        }
    }
}
