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
using PartnerBot.Core.Utils;
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
                        $"`add-embed`, `edit-embed`, `remove-embed`, `banner`, `color`, `tags`, `vanity`\n\n" +
                        $"*Server Settings:*\n" +
                        $"`get-nsfw`, `set-nsfw`";

        public SetupCommand(IServiceProvider services, DonorService donor,
            PartnerManagerService partners, GuildBanService ban,
            DiscordRestClient rest)
        {
            this._services = services;
            this._donor = donor;
            this._partners = partners;
            this._ban = ban;
            this._rest = rest;
        }

        [Command("setup")]
        [Description("Interactive Setup for Partner Bot")]
        [RequireServerAdminOrOwner]
        public async Task InteractiveSetupCommandAsync(CommandContext ctx)
        {
            // Check for donor rank updates
            // then display welcome message
            // along with donor features this server gets
            // or an advert for unlocking the donor features
            // (check, cross, and lock symbols next to features for avalible, used, and need to buy stuff)
            // have setup requirements displayed
            // and options to setup parts of the message
            // along with a toggle option, once everything is setup
            // once setup is closed, save new data.

            GuildBan? ban;
            if((ban = await this._ban.GetBanAsync(ctx.Guild.Id)) is not null)
            {
                await RespondError($"Your server is banned due to: {ban.Reason}\n\n" +
                    $"Contact a staff member on the [support server](https://discord.gg/3SCTnhCMam) to learn more.");

                await ctx.Guild.LeaveAsync();
                return;
            }

            using var scope = this._services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PartnerDatabaseContext>();
            Partner? partner = await db.Partners.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == ctx.Guild.Id);

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
            partner.DonorRank = await this._donor.GetDonorRankAsync(partner.OwnerId);

            await db.SaveChangesAsync();

            partner.ModifyToDonorRank();

            DiscordChannel? channel = null;
            ulong oldChannelId = 0;
            DiscordWebhook? hook = null;

            if(!string.IsNullOrWhiteSpace(partner.WebhookToken))
            {
                try
                {
                    hook = await ctx.Client.GetWebhookAsync(partner.WebhookId);
                }
                catch 
                {
                    hook = null;
                }
                channel = await ctx.Client.GetChannelAsync(hook.ChannelId);
                oldChannelId = channel.Id;
            }

            var statusEmbed = new DiscordEmbedBuilder(this.SetupBase);
            DSharpPlus.Interactivity.InteractivityExtension? interact = ctx.Client.GetInteractivity();
            bool done = false;
            bool errored = false;
            DiscordMessage? requirementsMessage = null;
            DiscordMessage? statusMessage = null;
            HashSet<string>? tagUpdate = null;
            do
            {
                DiscordEmbedBuilder? requirementsEmbed = await GetRequiermentsEmbed(partner, channel);

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

                (DSharpPlus.Interactivity.InteractivityResult<DiscordMessage>, bool) response = await GetFollowupMessageAsync(interact);

                if (!response.Item2) return;

                DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> res = response.Item1;

                string? msg = res.Result.Content;

                string? trimed = msg.ToLower().Trim();

                switch (trimed)
                {
                    // Exit calls.
                    case "exit":
                        await RespondError("Aborting...");
                        return;

                    case "save":
                        done = true;
                        break;

                    // Primary Settings.
                    case "channel":
                        ((DiscordChannel, DiscordWebhook, string)?, string?, bool) chanRes = await GetNewPartnerChannelAsync(partner, statusMessage, statusEmbed);
                        if (chanRes.Item3) return;

                        if (chanRes.Item1 is null)
                        {
                            await RespondError(chanRes.Item2 ?? "An unknown error occurred.");
                            return;
                        }

                        channel = chanRes.Item1.Value.Item1;
                        partner.WebhookId = chanRes.Item1.Value.Item2.Id;
                        partner.WebhookToken = chanRes.Item1.Value.Item2.Token;
                        partner.Invite = chanRes.Item1.Value.Item3;
                        break;

                    case "message":
                        (string?, string?, bool) messageRes = await GetNewMessage(partner, statusMessage, statusEmbed, partner.Message.GetUrls().Count);
                        if (messageRes.Item3) return;

                        if (messageRes.Item1 is null)
                        {
                            await RespondError(messageRes.Item2 ?? "An unknown error occurred.");
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

                    // Secondary Settings.
                    case "add-embed":
                        if (partner.DonorRank < DonorService.EMBED_LIMIT)
                        {
                            statusEmbed.WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**You need to be a Tripple Partner to use custom embeds! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get access to embeds.**")
                                .WithColor(DiscordColor.DarkRed);

                            errored = true;
                        }
                        else if (partner.DonorRank >= DonorService.HIGHEST_RANK 
                            ? partner.MessageEmbeds.Count >= DonorService.QUADRUPLE_EMBEDS 
                            : partner.MessageEmbeds.Count >= DonorService.TRIPPLE_EMBEDS)
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**You have used up all your embeds! You can edit a existing one, or remove an old one and add a new one.**")
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

                            (DSharpPlus.Interactivity.InteractivityResult<DiscordMessage>, bool) editStart = await GetFollowupMessageAsync(interact);

                            if (!editStart.Item2) return;

                            DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> startRes = editStart.Item1;

                            string? title = startRes.Result.Content.Trim();

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
                                (DiscordEmbedBuilder?, string?, bool) addEnd = await GetCustomDiscordEmbedAsync(partner, statusMessage, statusEmbed, title);

                                if (addEnd.Item3) return;

                                if (addEnd.Item1 is null)
                                {
                                    await RespondError(addEnd.Item2 ?? "An unknown error occurred.");
                                    return;
                                }

                                partner.MessageEmbeds.Add(addEnd.Item1);
                            }
                        }
                        break;

                    case "edit-embed":
                        if (partner.DonorRank < DonorService.EMBED_LIMIT)
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**You need to be a Tripple Partner to use custom embeds! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get access to embeds.**")
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
                            List<string> dat = new();
                            int i = 0;
                            foreach (DiscordEmbed? e in partner.MessageEmbeds)
                                dat.Add($"[{i++}] {e.Title}");

                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**Please enter the index of the embed you would like to edit:**\n" +
                                $"*[index] title*\n\n" +
                                $"{string.Join("\n", dat)}")
                                .WithColor(Color_PartnerBotMagenta);

                            await statusMessage.ModifyAsync(statusEmbed.Build());

                            (DSharpPlus.Interactivity.InteractivityResult<DiscordMessage>, bool) addStart = await GetFollowupMessageAsync(interact);

                            if (!addStart.Item2) return;

                            DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> startRes = addStart.Item1;

                            string? indexRaw = startRes.Result.Content.Trim();

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
                                DiscordEmbed? oldEmbed = partner.MessageEmbeds[index];

                                (DiscordEmbedBuilder?, string?, bool) editEnd = await GetCustomDiscordEmbedAsync(partner, statusMessage, statusEmbed, oldEmbed.Title, new(oldEmbed));

                                if (editEnd.Item3) return;

                                if (editEnd.Item1 is null)
                                {
                                    await RespondError(editEnd.Item2 ?? "An unknown error occurred.");
                                    return;
                                }

                                partner.MessageEmbeds[index] = editEnd.Item1;
                            }
                        }
                        break;

                    case "remove-embed":
                        if (partner.DonorRank < DonorService.EMBED_LIMIT)
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**You need to be a Tripple Partner to use custom embeds! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get access to embeds.**")
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
                            List<string> dat = new();
                            int i = 0;
                            foreach (DiscordEmbed? e in partner.MessageEmbeds)
                                dat.Add($"[{i++}] {e.Title}");

                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                .WithDescription($"{BASE_MESSAGE}\n\n" +
                                $"**Please enter the index of the embed you would like to remove:**\n" +
                                $"*[index] title*\n\n" +
                                $"{string.Join("\n", dat)}")
                                .WithColor(Color_PartnerBotMagenta);

                            await statusMessage.ModifyAsync(statusEmbed.Build());

                            (DSharpPlus.Interactivity.InteractivityResult<DiscordMessage>, bool) addStart = await GetFollowupMessageAsync(interact);

                            if (!addStart.Item2) return;

                            DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> startRes = addStart.Item1;

                            string? indexRaw = startRes.Result.Content.Trim();

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
                        break;

                    case "banner":
                        (Uri?, string?, bool) bannerRes = await GetNewPartnerBanner(statusMessage, statusEmbed);
                        if (bannerRes.Item3) return;

                        if (bannerRes.Item1 is null)
                        {
                            await RespondError(bannerRes.Item2 ?? "An unknown error occurred.");
                            return;
                        }

                        partner.Banner = bannerRes.Item1.AbsoluteUri;
                        break;

                    case "color":
                    case "colour":
                        (DiscordColor?, string?, bool) colorRes = await GetCustomEmbedColorAsync(partner, statusMessage, statusEmbed);
                        if (colorRes.Item3) return;

                        if (colorRes.Item1 is null)
                        {
                            await RespondError(colorRes.Item2 ?? "An unknown error occurred.");
                            return;
                        }

                        partner.BaseColor = colorRes.Item1.Value;
                        break;

                    case "tag":
                    case "tags":
                        (HashSet<string>?, string?, bool) tagRes = await UpdateTagsAsync(partner, statusMessage, statusEmbed);
                        if (tagRes.Item3) return;

                        if (tagRes.Item1 is null)
                        {
                            await RespondError(tagRes.Item2 ?? "An unknown error occurred.");
                            return;
                        }

                        tagUpdate = tagRes.Item1;
                        break;

                    case "vanity":
                        if (partner.DonorRank >= DonorService.VANITY_LIMIT)
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
                                    vanity = await this.Context.Guild.GetVanityInviteAsync();
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
                    case "get-nsfw":
                        if(!partner.NSFW)
                            partner.ReceiveNSFW = !partner.ReceiveNSFW;
                        else
                        {
                            statusEmbed.WithTitle("Partner Bot Setup - Main")
                                        .WithDescription($"{BASE_MESSAGE}\n\n" +
                                        $"**If your server is marked NSFW you must be allwoed to receive other NSFW server advertisments." +
                                        $" Make your advertisment *not* NSFW, then use `set-nsfw` again before turning this option off with" +
                                        $" `get-nsfw`.**")
                                        .WithColor(DiscordColor.DarkRed);

                            errored = true;
                        }
                        break;
                    case "set-nsfw":
                        partner.NSFW = !partner.NSFW;
                        if (partner.NSFW)
                            partner.ReceiveNSFW = true;
                        break;
                    default:
                        break;
                }

            } while (!done);

            await requirementsMessage.DeleteAsync();

            (Partner?, string) updateRes = await this._partners.UpdateOrAddPartnerAsync(ctx.Guild.Id, () =>
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
                    .WithDescription("An error occurred while saving:\n\n```\n" +
                    $"{updateRes.Item2}" +
                    $"\n```")
                    .WithColor(DiscordColor.DarkRed);
            }

            await statusMessage.ModifyAsync(statusEmbed.Build());
        }

        public async Task<DiscordEmbedBuilder> GetRequiermentsEmbed(Partner partner, DiscordChannel? channel = null)
        {
            // Required: message, channel.
            // Optional: banner, embed, links.
            bool validChannel = false;
            if (channel is not null)
                validChannel = GuildVerificationService.VerifyChannel(channel);
            bool invalidMessage = string.IsNullOrWhiteSpace(partner.Message);
            bool invalidBanner = string.IsNullOrWhiteSpace(partner.Banner);
            bool maxLinks = partner.LinksUsed >= DonorService.HIGHEST_RANK;
            bool linkCap = partner.LinksUsed >= partner.DonorRank;
            int maxEmbeds = partner.DonorRank == 2 ? DonorService.TRIPPLE_EMBEDS : partner.DonorRank >= 3 ? DonorService.QUADRUPLE_EMBEDS : 0;
            bool embedsRemaining = partner.MessageEmbeds.Count < maxEmbeds;
            bool embedAllowed = partner.DonorRank >= DonorService.EMBED_LIMIT && !(!embedsRemaining && partner.DonorRank == DonorService.EMBED_LIMIT);
            bool defaultColor = partner.BaseColor.Value == DiscordColor.Gray.Value;
            bool usedTags = partner.Tags.Count >= TAG_LIMIT;
            bool usedVanity = partner.VanityInvite is not null;

            DiscordInvite? vanity;
            try
            {
                vanity = await this.Context.Guild.GetVanityInviteAsync();
            }
            catch { vanity = null; }

            bool hasVanity =  vanity is not null;
            bool canUseVanity = partner.DonorRank >= 1;

            DiscordEmbedBuilder? requirementsEmbed = new DiscordEmbedBuilder()
                .WithColor(partner.BaseColor)
                .WithTitle("Partner Bot Setup Requirements")
                .AddField($"{(partner.Active ? this.Check.GetDiscordName() : this.Cross.GetDiscordName())} Active",
                    partner.Active ? "Partner Bot is **Active** on this server!" : "Partner Bot is **Inactive** on this server." +
                    " Complete the required options then `toggle` to activate Partner Bot!", false)
                .AddField("_ _", "``` ```", false)
                .AddField("**Required Settings**", "_ _")
                .AddField($"{(validChannel ? this.Check.GetDiscordName() : this.Cross.GetDiscordName())} Channel",
                    validChannel ? $"The current channel ({channel?.Mention}) is valid! Change it with `channel`." : "Please set a valid Partner Channel with `channel`.", true)
                .AddField($"{(invalidMessage ? this.Cross.GetDiscordName() : this.Check.GetDiscordName())} Message", 
                    invalidMessage ? "You have no message set! Set one with `message`." : "Your message is set. Change it with `message`.", true)
                .AddField("_ _", "``` ```", false)
                .AddField("**Optional Settings**", "_ _", false)
                .AddField($"{(invalidBanner ? this.Cross.GetDiscordName() : this.Check.GetDiscordName())} Banner", 
                    invalidBanner ? "You have no banner set! Set one with `banner`." : "You have a banner set! Change it with `banner`.", true)
                .AddField($"{(linkCap ? (maxLinks ? this.Check.GetDiscordName() : this.Lock.GetDiscordName()) : this.Cross.GetDiscordName())} Links", 
                    linkCap ? 
                        (maxLinks ? "You have used all your links! Modify your message to change the links with `message`." 
                            : "You can't use any more links! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get more links.")
                        : $"You have used {partner.LinksUsed} of {partner.DonorRank} avalible links. Edit your message with `message` to use them!", true)
                .AddField($"{(embedAllowed ? (embedsRemaining ? this.Cross.GetDiscordName() : this.Check.GetDiscordName()) : this.Lock.GetDiscordName())} Embeds",
                    embedAllowed ?
                        (embedsRemaining ? $"You have used {partner.MessageEmbeds.Count} of {maxEmbeds} embeds. Use `add-embed` to add a new embed!"
                            : "You have used all of your embeds! Consider editing or removing some to update your message with `edit-embed` or `remove-embed`!")
                        : "You can't add any embeds! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get access to more embeds.", true)
                .AddField($"{(defaultColor ? this.Cross.GetDiscordName() : this.Check.GetDiscordName())} Color",
                    defaultColor ? "You have no custom color set! Set one with `color`."
                        : $"You have your custom color set to `R{partner.BaseColor.R}, G{partner.BaseColor.G}, B{partner.BaseColor.B}`! Change it with `color`.", true)
                .AddField($"{(usedTags ? this.Check.GetDiscordName() : this.Cross.GetDiscordName())} Tags",
                    usedTags ? $"You have used all {TAG_LIMIT} of your tags. Edit your current tags with `tags`!"
                        : $"You have used {partner.Tags.Count} of your {TAG_LIMIT} avalible tags. Add some with `tags`!", true)
                .AddField($"{(canUseVanity ? (hasVanity ? (usedVanity ? this.Check.GetDiscordName() : this.Cross.GetDiscordName()) : this.Check.GetDiscordName()) : this.Lock.GetDiscordName())} Vanity Invite",
                canUseVanity ?
                    (hasVanity ? 
                        (usedVanity ? "You have enabled your vanity URL! Want to disable it? Use `vanity`!" : "Want to use your vanity URL? Use `vanity`!")
                    : "You don't have a vanity URL for your server!")
                : $"You can't use a vanity URL! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to use your servers vanity URL with Partner Bot" +
                $" (You must have a vanity URL from Discord to use this option).", true)
                .AddField("_ _", "``` ```", false)
                .AddField("**Server Settings**", "_ _", false)
                .AddField($"{(partner.NSFW ? this.Yes.GetDiscordName() : this.No.GetDiscordName())} NSFW Server", 
                    partner.NSFW ? "Your server is marked as NSFW. **This does not allow NSFW content in your banner or advertisment!**. This means your" +
                    " server advertisment is mentioning NSFW content, but does not include it. Use `set-nsfw` to toggle this option."
                        : "Your server is **not** marked as NSFW. No mention of NSFW content can be in your advertisment. Use `set-nsfw` to toggle this option.", true)
                .AddField($"{(partner.ReceiveNSFW ? this.Yes.GetDiscordName() : this.No.GetDiscordName())} Get NSFW Server Adverts", 
                    partner.ReceiveNSFW ? "Your server is allowed to receive advertisments from NSFW servers. This means advertisments mentioning NSFW" +
                    " content are allowed. **Please report any advertisments with NSFW images.** Use `get-nsfw` to toggle this option."
                        : "Your server is **not** allowed to receive advertisments from NSFW servers. **Please report any advertisments that are or mention" +
                        " NSFW content that you receive.** Use `get-nsfw` to toggle this option.", true);


            return requirementsEmbed;
        }
    }
}
