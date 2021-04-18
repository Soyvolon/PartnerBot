using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;

using PartnerBot.Core.Services;

namespace PartnerBot.Core.Entities
{
    /// <summary>
    /// The data class for a Partner Match. Contains data for both the local Partner (parent class) and the match.
    /// </summary>
    public class PartnerData : Partner
    {
        public Partner Match { get; internal set; }
        public Partner? ExtraMessage { get; internal set; }

        public PartnerData(Partner self, Partner match, Partner? extra = null)
        {
            foreach (System.Reflection.PropertyInfo? prop in self.GetType().GetProperties())
            {
                GetType().GetProperty(prop.Name)?.SetValue(this, prop.GetValue(self, null), null);
            }

            this.Match = match;
            this.ExtraMessage = extra;
        }

        public async Task ExecuteAsync(DiscordRestClient rest, PartnerSenderArguments senderArguments)
        {
            if (senderArguments.DevelopmentStressTest)
            {
                await ExecuteStressTestMessage(rest);
                return;
            }

            bool vanity = this.Match.VanityInvite is not null && this.Match.DonorRank >= DonorService.VANITY_LIMIT;
            bool attachEmbeds = this.Match.DonorRank >= DonorService.EMBED_LIMIT;

            DiscordWebhookBuilder? hook = new DiscordWebhookBuilder()
                .WithContent($"{this.Match.Message}\n\n" +
                $"https://discord.gg/" +
                $"{(vanity ? this.Match.VanityInvite : this.Match.Invite)}")
                .WithUsername($"{this.Match.GuildName} | Partner Bot");

            if (attachEmbeds)
            {
                if (this.Match.DonorRank >= DonorService.HIGHEST_RANK)
                    hook.AddEmbeds(this.Match.MessageEmbeds);
                else if(this.Match.MessageEmbeds.Count > 0)
                    hook.AddEmbed(this.Match.MessageEmbeds[0]);
            }

            hook.AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Gray)
                    .WithTitle("Partner Bot Advertisment")
                    .WithDescription($"**ID:** {this.Match.GuildId}")
                    .WithImageUrl(this.Match.Banner));

            if (!string.IsNullOrWhiteSpace(this.Match.GuildIcon))
                hook.WithAvatarUrl(this.Match.GuildIcon);

            await rest.ExecuteWebhookAsync(this.WebhookId, this.WebhookToken, hook);

            if(this.ExtraMessage is not null)
            {
                var eDat = new PartnerData(this, this.ExtraMessage);

                await eDat.ExecuteAsync(rest, senderArguments);
            }
        }

        private async Task ExecuteStressTestMessage(DiscordRestClient rest)
        {
            DiscordWebhookBuilder? hook = new DiscordWebhookBuilder()
                .WithContent($"This Channel: <#{this.GuildId}>" +
                "\n```\n" +
                "STRESS TEST\n\n" +
                $"This Partner ({this.GuildName}):\n" +
                $"ID: {this.GuildId}\n" +
                $"Tags: {string.Join(", ", this.Tags)}\n" +
                $"Size: {this.UserCount}\n" +
                $"Donor Rank: {this.DonorRank}\n\n" +
                $"Other Partner ({this.Match.GuildName}):\n" +
                $"ID: {this.Match.GuildId}\n" +
                $"Tags: {string.Join(", ", this.Match.Tags)}\n" +
                $"Size: {this.Match.UserCount}\n" +
                $"Donor Rank: {this.Match.DonorRank}\n" +
                $"Extra: {this.ExtraMessage}" +
                $"\n```\n" +
                $"Other Channel: <#{this.Match.GuildId}>")
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Gray)
                    .WithTitle("Partner Bot Advertisment")
                    .WithDescription($"**ID:** {this.Match.GuildId}")
                    .WithImageUrl(this.Match.Banner)
                );

            await rest.ExecuteWebhookAsync(this.WebhookId, this.WebhookToken, hook);
        }
    }
}
