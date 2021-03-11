using System;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using Microsoft.Extensions.DependencyInjection;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;
using PartnerBot.Core.Services;
using PartnerBot.Discord.Commands.Conditions;

namespace PartnerBot.Discord.Commands.Core
{
    public class SetupCommand : CoreCommandModule
    {
        private readonly IServiceProvider _services;
        private readonly DonorService _donor;

        public SetupCommand(IServiceProvider services, DonorService donor)
        {
            _services = services;
            _donor = donor;
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
            // (check, cross, and lock symbols next to features for avalible, used, and need to buy stuff?)
            // ... have setup requirements displayed ...
            // ... and options to setup parts of the message ...
            // ... along with a toggle option, once everything is setup ...
            // ... once setup is closed, save new data.

            var db = _services.GetRequiredService<PartnerDatabaseContext>();
            var partner = await db.FindAsync<Partner>(ctx.Guild.Id);

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
            partner.DonorRank = await _donor.GetDonorRank(partner);

            await db.SaveChangesAsync();

            bool done = false;
            DiscordMessage? requirementsMessage = null;
            do
            {
                var requirementsEmbed = GetRequiermentsEmbed(partner);

                if (requirementsMessage is null)
                    requirementsMessage = await ctx.RespondAsync(requirementsEmbed);
                else await requirementsMessage.ModifyAsync(requirementsEmbed.Build());

            } while (!done);
        }

        public static DiscordEmbedBuilder GetRequiermentsEmbed(Partner partner, bool validChannel)
        {
            // Required: message, channel
            // Optional: banner, embed, links

            bool invalidMessage = string.IsNullOrWhiteSpace(partner.Message);
            bool invalidBanner = string.IsNullOrWhiteSpace(partner.Banner);
            bool maxLinks = partner.LinksUsed >= 3;
            bool linkCap = partner.LinksUsed >= partner.DonorRank;
            bool maxEmbeds = partner.MessageEmbeds.Count >= 4;
            bool embedAllowed = partner.DonorRank >= 3;

            var requirementsEmbed = new DiscordEmbedBuilder()
                .WithColor(Color_PartnerBotMagenta)
                .WithTitle("Partner Bot Setup Requirements")
                .AddField($"{(partner.Active ? Check.GetDiscordName() : Cross.GetDiscordName())} Active",
                    partner.Active ? "Partner Bot is **Active** on this server!" : "Partner Bot is **Inactive** on this server." +
                    " Complete the required options then `toggle` to activate Partner Bot!", false)
                .AddField("**Required Settings**", "``` ```")
                .AddField($"{(validChannel ? Check.GetDiscordName() : Cross.GetDiscordName())} Channel",
                    validChannel ? "The current channel is valid! Chane it with `channel`." : "Please set a valid Partner Channel with `channel`.", true)
                .AddField($"{(invalidMessage ? Cross.GetDiscordName() : Check.GetDiscordName())} Message", 
                    invalidMessage ? "You have no message set! Set one with `message`." : "Your message is set. Change it with `message`.", true)
                .AddField("**Optional Settings**", "``` ```", false)
                .AddField($"{(invalidBanner ? Cross.GetDiscordName() : Check.GetDiscordName())}", 
                    invalidBanner ? "You have no banner set! Set one with `banner`." : "You have a banner set! Change it with `banner`.", true)
                .AddField($"{(linkCap ? (maxLinks ? Check.GetDiscordName() : Lock.GetDiscordName()) : Cross.GetDiscordName())} Links", 
                    linkCap ? 
                        (maxLinks ? "You have used all your links! Modify your message to change the links with `message`." 
                            : "You can't use any more links! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get more links.")
                        : $"You have used {partner.LinksUsed} of {partner.DonorRank} avalible links. Edit your message with `message` to use them!", true)
                .AddField($"{(maxEmbeds ? (embedAllowed ? Cross.GetDiscordName() : Lock.GetDiscordName()) : Check.GetDiscordName())} Embeds", 
                    maxEmbeds ?
                        (embedAllowed ? $"You have used {partner.MessageEmbeds.Count} of 4 embeds. Use `add-embed` to add a new embed!"
                            : "You can't add any embeds! Consider [donating](https://www.patreon.com/cessumdevelopment?fan_landing=true) to get access to embeds.")
                        : "You have used all of your embeds! Consider editing or removing some to update your message with `edit-embed` or `remove-embed`!", true);
            
            return requirementsEmbed;
        }
    }
}
