using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;
using PartnerBot.Core.Entities.Moderation;
using PartnerBot.Core.Services;
using PartnerBot.Discord.Commands.Conditions;

namespace PartnerBot.Discord.Commands.Core
{
    public partial class SetupCommand : CoreCommandModule
    {
        private readonly IServiceProvider _services;
        private readonly DonorService _donor;
        private readonly PartnerManagerService _partners;
        private readonly GuildBanService _ban;
        private readonly DiscordRestClient _rest;
        private static readonly string BASE_MESSAGE = $"**Setup Options:**\n\n" +
                        $"*Main Options:*\n" +
                        $"`channel`, `message`, `toggle`, `save`\n\n" +
                        $"*Optional Options:*\n" +
                        $"`add-embed`, `edit-embed`, `remove-embed`, `banner`, `color`, `tags`, `vanity`";

        public SetupCommand(IServiceProvider services, DonorService donor,
            PartnerManagerService partners, GuildBanService ban,
            DiscordRestClient rest)
        {
            _services = services;
            _donor = donor;
            _partners = partners;
            _ban = ban;
            _rest = rest;
        }

        [Command("setup")]
        [Description("Interactive Setup for Partner Bot")]
        [RequireServerAdminOrOwner]
        public async Task InteractiveSetupCommandAsync(CommandContext ctx)
        {
            // Check for donor rank updates ...
            // ... then display welcome message ...
            // ... along with donor features this server gets ...
            // ... or an advert for unlocking the donor features ...
            // (check, cross, and lock symbols next to features for avalible, used, and need to buy stuff)
            // ... have setup requirements displayed ...
            // ... and options to setup parts of the message ...
            // ... along with a toggle option, once everything is setup ...
            // ... once setup is closed, save new data.

            GuildBan? ban;
            if((ban = await _ban.GetBanAsync(ctx.Guild.Id)) is not null)
            {
                await RespondError($"Your server is banned due to: {ban.Reason}\n\n" +
                    $"Contact a staff member on the [support server](https://discord.gg/3SCTnhCMam) to learn more.");

                await ctx.Guild.LeaveAsync();
                return;
            }

            var db = _services.GetRequiredService<PartnerDatabaseContext>();
            var partner = await db.Partners.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == ctx.Guild.Id);

            if(partner is null)
            {
                partner = new()
                {
                    GuildId = ctx.Guild.Id,
                };

                await db.AddAsync(partner);
                await db.SaveChangesAsync();
            }

            partner.OwnerId = ctx.Guild.OwnerId;
            partner.GuildName = ctx.Guild.Name;
            partner.GuildIcon = ctx.Guild.IconUrl;
            partner.UserCount = ctx.Guild.MemberCount;
            partner.DonorRank = await _donor.GetDonorRankAsync(partner.OwnerId);

            await db.SaveChangesAsync();

            DiscordChannel? channel = null;
            ulong oldChannelId = 0;
            DiscordWebhook? hook = null;

            if(!string.IsNullOrWhiteSpace(partner.WebhookToken))
            {
                hook = await ctx.Client.GetWebhookAsync(partner.WebhookId);
                channel = await ctx.Client.GetChannelAsync(hook.ChannelId);
                oldChannelId = channel.Id;
            }

            var statusEmbed = new DiscordEmbedBuilder(SetupBase);
            var interact = ctx.Client.GetInteractivity();
            bool done = false;
            bool errored = false;
            DiscordMessage? requirementsMessage = null;
            DiscordMessage? statusMessage = null;
            HashSet<string>? tagUpdate = null;
            do
            {
                var requirementsEmbed = await GetRequiermentsEmbed(partner, channel);

                if (requirementsMessage is null)
                    requirementsMessage = await ctx.RespondAsync(requirementsEmbed);
                else await requirementsMessage.ModifyAsync(requirementsEmbed.Build());

                if (!errored)
                {
                    statusEmbed.WithDescription(BASE_MESSAGE)
                        .WithColor(Color_PartnerBotMagenta);
                }

                errored = false;

                if (statusMessage is null)
                    statusMessage = await ctx.RespondAsync(statusEmbed);
                else await statusMessage.ModifyAsync(statusEmbed.Build());

                var response = await GetFollowupMessageAsync(interact);

                if (!response.Item2) return;

                var res = response.Item1;

                var msg = res.Result.Content;

                var trimed = msg.ToLower().Trim();

                switch (trimed)
                {
                    // Exit calls
                    case "exit":
                        await RespondError("Aborting...");
                        return;

                    case "save":
                        done = true;
                        break;

                    // Primary Settings
                    case "channel":
                        var chanRes = await GetNewPartnerChannelAsync(partner, statusMessage, statusEmbed);
                        if (chanRes.Item3) return;

                        if (chanRes.Item1 is null)
                        {
                            await RespondError(chanRes.Item2 ?? "An unknown error occoured.");
                            return;
                        }

                        channel = chanRes.Item1.Value.Item1;
                        partner.WebhookId = chanRes.Item1.Value.Item2.Id;
                        partner.WebhookToken = chanRes.Item1.Value.Item2.Token;
                        partner.Invite = chanRes.Item1.Value.Item3;
                        break;

                    case "message":
                        var messageRes = await GetNewMessage(partner, statusMessage, statusEmbed);
                        if (messageRes.Item3) return;

                        if (messageRes.Item1 is null)
                        {
                            await RespondError(messageRes.Item2 ?? "An unknown error occoured.");
                            return;
                        }

                        partner.Message = messageRes.Item1;
                        break;

                    case "toggle":
                        if (!partner.Active && partner.IsSetup())
                            partner.Active = true;
                        else if (partner.Active)
                            partner.Active = false;
                        else
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**Core setup is not complete. Please complete the required settings before toggling Partner Bot**")
                                .WithColor(DiscordColor.DarkRed);

                            errored = true;
                        }
                        break;

                    // Secondary Settings
                    case "add-embed":
                        if (partner.DonorRank < 3)
                        {
                            statusEmbed.WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**You need to be a Quadruple Partner to use custom embeds! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get access to embeds.**")
                                .WithColor(DiscordColor.DarkRed);

                            errored = true;
                        }
                        else if (partner.MessageEmbeds.Count <= 0)
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**There are no embeds to edit!**")
                                .WithColor(DiscordColor.DarkRed);

                            errored = true;
                        }
                        else
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**Please enter the title for your new embed:**")
                                .WithColor(Color_PartnerBotMagenta);

                            await statusMessage.ModifyAsync(statusEmbed.Build());

                            var editStart = await GetFollowupMessageAsync(interact);

                            if (editStart.Item2) return;

                            var startRes = editStart.Item1;

                            var title = startRes.Result.Content.Trim();

                            if (title.Length > 256)
                            {
                                statusEmbed.WithTitle("Partner Bot Setup - Main")
                                    .WithDescription($"{BASE_MESSAGE}\n\n" +
                                    $"**The embed title cannot be longer than 256 characters. Returning to main menu.**")
                                    .WithColor(DiscordColor.DarkRed);

                                errored = true;
                            }
                            else
                            {
                                var addEnd = await GetCustomDiscordEmbedAsync(partner, statusMessage, statusEmbed, title);

                                if (addEnd.Item3) return;

                                if (addEnd.Item1 is null)
                                {
                                    await RespondError(addEnd.Item2 ?? "An unknown error occoured.");
                                    return;
                                }

                                partner.MessageEmbeds.Add(addEnd.Item1);
                            }
                        }
                        break;

                    case "edit-embed":
                        if (partner.DonorRank < 3)
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**You need to be a Quadruple Partner to use custom embeds! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get access to embeds.**")
                                .WithColor(DiscordColor.DarkRed);

                            errored = true;
                        }
                        else if (partner.MessageEmbeds.Count < DonorService.MAX_EMBEDS)
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**You have used up all your embeds! You can edit a exsisting one, or remove an old one and add a new one.**")
                                .WithColor(DiscordColor.DarkRed);

                            errored = true;
                        }
                        else
                        {
                            List<string> dat = new();
                            int i = 0;
                            foreach (var e in partner.MessageEmbeds)
                                dat.Add($"[{i++}] {e.Title}");

                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**Please enter the index of the emebed you would like to edit:**\n" +
                                $"*[index] title*\n\n" +
                                $"{string.Join("\n", dat)}")
                                .WithColor(Color_PartnerBotMagenta);

                            await statusMessage.ModifyAsync(statusEmbed.Build());

                            var addStart = await GetFollowupMessageAsync(interact);

                            if (addStart.Item2) return;

                            var startRes = addStart.Item1;

                            var indexRaw = startRes.Result.Content.Trim();

                            if (!int.TryParse(indexRaw, out int index))
                            {
                                statusEmbed.WithTitle("Partner Bot Setup - Main")
                                    .WithDescription($"{BASE_MESSAGE}\n\n" +
                                    $"**The value provided was not a number! Returning to main menu.**")
                                    .WithColor(DiscordColor.DarkRed);

                                errored = true;
                            }
                            else if (index > partner.MessageEmbeds.Count || index < 0)
                            {
                                statusEmbed.WithTitle("Partner Bot Setup - Main")
                                    .WithDescription($"{BASE_MESSAGE}\n\n" +
                                    $"**The value provided was not a valid embed! Returning to main menu.**")
                                    .WithColor(DiscordColor.DarkRed);

                                errored = true;
                            }
                            else
                            {
                                var oldEmbed = partner.MessageEmbeds[index];

                                var editEnd = await GetCustomDiscordEmbedAsync(partner, statusMessage, statusEmbed, oldEmbed.Title, oldEmbed);

                                if (editEnd.Item3) return;

                                if (editEnd.Item1 is null)
                                {
                                    await RespondError(editEnd.Item2 ?? "An unknown error occoured.");
                                    return;
                                }

                                partner.MessageEmbeds[index] = editEnd.Item1;
                            }
                        }
                        break;

                    case "remove-embed":
                        if (partner.DonorRank < 3)
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**You need to be a Quadruple Partner to use custom embeds! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get access to embeds.**")
                                .WithColor(DiscordColor.DarkRed);

                            errored = true;
                        }
                        else if (partner.MessageEmbeds.Count <= 0)
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**There are no embeds to remove!**")
                                .WithColor(DiscordColor.DarkRed);

                            errored = true;
                        }
                        else
                        {
                            if (partner.DonorRank < 3)
                            {
                                statusEmbed.WithTitle("Partner Bot Setup - Main")
                                    .WithDescription($"{BASE_MESSAGE}\n\n" +
                                    $"**You need to be a Quadruple Partner to use custom embeds! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get access to embeds.**")
                                    .WithColor(DiscordColor.DarkRed);

                                errored = true;
                            }
                            else if (partner.MessageEmbeds.Count < DonorService.MAX_EMBEDS)
                            {
                                statusEmbed.WithTitle("Partner Bot Setup - Main")
                                    .WithDescription($"{BASE_MESSAGE}\n\n" +
                                    $"**You have used up all your embeds! You can edit a exsisting one, or remove an old one and add a new one.**")
                                    .WithColor(DiscordColor.DarkRed);

                                errored = true;
                            }
                            else
                            {
                                List<string> dat = new();
                                int i = 0;
                                foreach (var e in partner.MessageEmbeds)
                                    dat.Add($"[{i++}] {e.Title}");

                                statusEmbed.WithTitle("Partner Bot Setup - Main")
                                    .WithDescription($"{BASE_MESSAGE}\n\n" +
                                    $"**Please enter the index of the emebed you would like to remove:**\n" +
                                    $"*[index] title*\n\n" +
                                    $"{string.Join("\n", dat)}")
                                    .WithColor(Color_PartnerBotMagenta);

                                await statusMessage.ModifyAsync(statusEmbed.Build());

                                var addStart = await GetFollowupMessageAsync(interact);

                                if (addStart.Item2) return;

                                var startRes = addStart.Item1;

                                var indexRaw = startRes.Result.Content.Trim();

                                if (!int.TryParse(indexRaw, out int index))
                                {
                                    statusEmbed.WithTitle("Partner Bot Setup - Main")
                                        .WithDescription($"{BASE_MESSAGE}\n\n" +
                                        $"**The value provided was not a number! Returning to main menu.**")
                                        .WithColor(DiscordColor.DarkRed);

                                    errored = true;
                                }
                                else if (index > partner.MessageEmbeds.Count || index < 0)
                                {
                                    statusEmbed.WithTitle("Partner Bot Setup - Main")
                                        .WithDescription($"{BASE_MESSAGE}\n\n" +
                                        $"**The value provided was not a valid embed! Returning to main menu.**")
                                        .WithColor(DiscordColor.DarkRed);

                                    errored = true;
                                }
                                else
                                {
                                    partner.MessageEmbeds.RemoveAt(index);

                                    statusEmbed.WithTitle("Partner Bot Setup - Main")
                                        .WithDescription($"{BASE_MESSAGE}\n\n" +
                                        $"**Embed removed.**");
                                }
                            }
                        }
                        break;

                    case "banner":
                        var bannerRes = await GetNewPartnerBanner(statusMessage, statusEmbed);
                        if (bannerRes.Item3) return;

                        if (bannerRes.Item1 is null)
                        {
                            await RespondError(bannerRes.Item2 ?? "An unknown error occoured.");
                            return;
                        }

                        partner.Banner = bannerRes.Item1.AbsolutePath;
                        break;

                    case "color":
                    case "colour":
                        var colorRes = await GetCustomEmbedColorAsync(partner, statusMessage, statusEmbed);
                        if (colorRes.Item3) return;

                        if (colorRes.Item1 is null)
                        {
                            await RespondError(colorRes.Item2 ?? "An unknown error occoured.");
                            return;
                        }

                        partner.BaseColor = colorRes.Item1.Value;
                        break;

                    case "tag":
                    case "tags":
                        var tagRes = await UpdateTagsAsync(partner, statusMessage, statusEmbed);
                        if (tagRes.Item3) return;

                        if (tagRes.Item1 is null)
                        {
                            await RespondError(tagRes.Item2 ?? "An unknown error occoured.");
                            return;
                        }

                        tagUpdate = tagRes.Item1;
                        break;

                    case "vanity":
                        if (partner.DonorRank >= 1)
                        {
                            if (partner.VanityInvite is not null)
                            {
                                partner.VanityInvite = null;

                                statusEmbed.WithTitle("Partner Bot Setup - Main")
                                            .WithDescription($"{BASE_MESSAGE}\n\n" +
                                            $"**Vanity Invite disabled!**")
                                            .WithColor(Color_PartnerBotMagenta);

                                errored = true;
                            }
                            else
                            {
                                DiscordInvite? vanity;
                                try
                                {
                                    vanity = await Context.Guild.GetVanityInviteAsync();
                                }
                                catch { vanity = null; }

                                if (vanity is not null)
                                {
                                    partner.VanityInvite = vanity.Code;

                                    statusEmbed.WithTitle("Partner Bot Setup - Main")
                                            .WithDescription($"{BASE_MESSAGE}\n\n" +
                                            $"**Vanity Invite enabled!**")
                                            .WithColor(Color_PartnerBotMagenta);

                                    errored = true;
                                }
                                else
                                {
                                    statusEmbed.WithTitle("Partner Bot Setup - Main")
                                            .WithDescription($"{BASE_MESSAGE}\n\n" +
                                            $"**You do not have a vanity invite for your server!**")
                                            .WithColor(DiscordColor.DarkRed);

                                    errored = true;
                                }
                            }
                        }
                        else
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                        .WithDescription($"{BASE_MESSAGE}\n\n" +
                                        $"**You do not have a high enough donor rank to use vanity invites." +
                                        $" Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true)" +
                                        $" to use your vanity invite!**")
                                        .WithColor(DiscordColor.DarkRed);

                            errored = true;
                        }
                        break;

                    default:
                        break;
                }

            } while (!done);

            await requirementsMessage.DeleteAsync();

            var updateRes = await _partners.UpdateOrAddPartnerAsync(ctx.Guild.Id, () =>
            {
                var update = PartnerUpdater.BuildFromPartner(partner);
                
                if(channel is not null)
                    update.ChannelId = channel.Id;

                if (tagUpdate is not null)
                    update.TagOverride = tagUpdate;

                return update;
            });

            if (updateRes.Item1 is not null)
            {
                statusEmbed.WithTitle("Partner Bot Setup - Main")
                    .WithDescription("Partner Setup Saved!")
                    .WithColor(DiscordColor.DarkGreen);
            }
            else
            {
                statusEmbed.WithTitle("Partner Bot Setup - Main")
                    .WithDescription("An error occoured while saving:\n\n```\n" +
                    $"{updateRes.Item2}" +
                    $"\n```")
                    .WithColor(DiscordColor.DarkRed);
            }

            await statusMessage.ModifyAsync(statusEmbed.Build());
        }

        public async Task<DiscordEmbedBuilder> GetRequiermentsEmbed(Partner partner, DiscordChannel? channel = null)
        {
            // Required: message, channel
            // Optional: banner, embed, links
            bool validChannel = false;
            if (channel is not null)
                validChannel = GuildVerificationService.VerifyChannel(channel);
            bool invalidMessage = string.IsNullOrWhiteSpace(partner.Message);
            bool invalidBanner = string.IsNullOrWhiteSpace(partner.Banner);
            bool maxLinks = partner.LinksUsed >= 3;
            bool linkCap = partner.LinksUsed >= partner.DonorRank;
            bool embedsRemaining = partner.MessageEmbeds.Count < DonorService.MAX_EMBEDS;
            bool embedAllowed = partner.DonorRank >= 3;
            bool defaultColor = partner.BaseColor.Value == DiscordColor.Gray.Value;
            bool usedTags = partner.Tags.Count >= TAG_LIMIT;
            bool usedVanity = partner.VanityInvite is not null;

            DiscordInvite? vanity;
            try
            {
                vanity = await Context.Guild.GetVanityInviteAsync();
            }
            catch { vanity = null; }

            bool hasVanity =  vanity is not null;
            bool canUseVanity = partner.DonorRank >= 1;

            var requirementsEmbed = new DiscordEmbedBuilder()
                .WithColor(Color_PartnerBotMagenta)
                .WithTitle("Partner Bot Setup Requirements")
                .AddField($"{(partner.Active ? Check.GetDiscordName() : Cross.GetDiscordName())} Active",
                    partner.Active ? "Partner Bot is **Active** on this server!" : "Partner Bot is **Inactive** on this server." +
                    " Complete the required options then `toggle` to activate Partner Bot!", false)
                .AddField("_ _", "``` ```", false)
                .AddField("**Required Settings**", "_ _")
                .AddField($"{(validChannel ? Check.GetDiscordName() : Cross.GetDiscordName())} Channel",
                    validChannel ? $"The current channel ({channel?.Mention}) is valid! Change it with `channel`." : "Please set a valid Partner Channel with `channel`.", true)
                .AddField($"{(invalidMessage ? Cross.GetDiscordName() : Check.GetDiscordName())} Message", 
                    invalidMessage ? "You have no message set! Set one with `message`." : "Your message is set. Change it with `message`.", true)
                .AddField("_ _", "``` ```", false)
                .AddField("**Optional Settings**", "_ _", false)
                .AddField($"{(invalidBanner ? Cross.GetDiscordName() : Check.GetDiscordName())} Banner", 
                    invalidBanner ? "You have no banner set! Set one with `banner`." : "You have a banner set! Change it with `banner`.", true)
                .AddField($"{(linkCap ? (maxLinks ? Check.GetDiscordName() : Lock.GetDiscordName()) : Cross.GetDiscordName())} Links", 
                    linkCap ? 
                        (maxLinks ? "You have used all your links! Modify your message to change the links with `message`." 
                            : "You can't use any more links! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get more links.")
                        : $"You have used {partner.LinksUsed} of {partner.DonorRank} avalible links. Edit your message with `message` to use them!", true)
                .AddField($"{(embedsRemaining ? (embedAllowed ? Cross.GetDiscordName() : Lock.GetDiscordName()) : Check.GetDiscordName())} Embeds", 
                    embedsRemaining ?
                        (embedAllowed ? $"You have used {partner.MessageEmbeds.Count} of 4 embeds. Use `add-embed` to add a new embed!"
                            : "You can't add any embeds! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get access to embeds.")
                        : "You have used all of your embeds! Consider editing or removing some to update your message with `edit-embed` or `remove-embed`!", true)
                .AddField($"{(defaultColor ? Cross.GetDiscordName() : Check.GetDiscordName())} Color",
                    defaultColor ? "You have no custom color set! Set one with `color`."
                        : $"You have your custom color set to `R{partner.BaseColor.R}, G{partner.BaseColor.G}, B{partner.BaseColor.B}`! Change it with `color`.", true)
                .AddField($"{(usedTags ? Check.GetDiscordName() : Cross.GetDiscordName())} Tags",
                    usedTags ? $"You have used all {TAG_LIMIT} of your tags. Edit your current tags with `tags`!"
                        : $"You have used {partner.Tags.Count} of your {TAG_LIMIT} avalible tags. Add some with `tags`!", true)
                .AddField($"{(canUseVanity ? (hasVanity ? (usedVanity ? Check.GetDiscordName() : Cross.GetDiscordName()) : Check.GetDiscordName()) : Lock.GetDiscordName())} Vanity Invite",
                canUseVanity ?
                    (hasVanity ? 
                        (usedVanity ? "You have enabled your vanity URL! Want to disable it? Use `vanity`!" : "Want to use your vanity URL? Use `vanity`!")
                    : "You don't have a vanity URL for your server!")
                : $"You can't use a vanity URL! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to use your servers vanity URL with Partner Bot" +
                $" (You must have a vanity URL from Discord to use this option).", true);


            return requirementsEmbed;
        }
    }
}
