using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;

namespace PartnerBot.Core.Entities
{
    public class PartnerData : Partner
    {
        public Partner Match { get; internal set; }
        public Partner? ExtraMessage { get; internal set; }

        public PartnerData(Partner self, Partner match, Partner? extra = null)
        {
            foreach (var prop in self.GetType().GetProperties())
            {
                this.GetType().GetProperty(prop.Name)?.SetValue(this, prop.GetValue(self, null), null);
            }

            Match = match;
            ExtraMessage = extra;
        }

        public async Task ExecuteAsync(DiscordRestClient rest, PartnerSenderArguments senderArguments)
        {
            if (senderArguments.DevelopmentStressTest)
            {
                await ExecuteStressTestMessage(rest);
                return;
            }

            bool vanity = Match.VanityInvite is not null && Match.DonorRank >= 1;
            bool attachEmbeds = Match.DonorRank >= 3;

            var hook = new DiscordWebhookBuilder()
                .WithContent($"{Match.Message}\n\n" +
                $"https://discord.gg/" +
                $"{(vanity ? Match.VanityInvite : Match.Invite)}")
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Gray)
                    .WithTitle("Partner Bot Advertisment")
                    .WithDescription($"**ID:** {Match.GuildId}")
                    .WithImageUrl(Match.Banner))
                .WithUsername($"{Match.GuildName} | Partner Bot");

            if (attachEmbeds)
                hook.AddEmbeds(Match.MessageEmbeds);

            if (!string.IsNullOrWhiteSpace(Match.GuildIcon))
                hook.WithAvatarUrl(Match.GuildIcon);

            await rest.ExecuteWebhookAsync(WebhookId, WebhookToken, hook);

            if(ExtraMessage is not null)
            {
                var eDat = new PartnerData(this, ExtraMessage);

                await eDat.ExecuteAsync(rest, senderArguments);
            }
        }

        private async Task ExecuteStressTestMessage(DiscordRestClient rest)
        {
            var hook = new DiscordWebhookBuilder()
                .WithContent($"This Channel: <#{GuildId}>" +
                "\n```\n" +
                "STRESS TEST\n\n" +
                $"This Partner ({GuildName}):\n" +
                $"ID: {GuildId}\n" +
                $"Tags: {string.Join(", ", Tags)}\n" +
                $"Size: {UserCount}\n" +
                $"Donor Rank: {DonorRank}\n\n" +
                $"Other Partner ({Match.GuildName}):\n" +
                $"ID: {Match.GuildId}\n" +
                $"Tags: {string.Join(", ", Match.Tags)}\n" +
                $"Size: {Match.UserCount}\n" +
                $"Donor Rank: {Match.DonorRank}\n" +
                $"Extra: {ExtraMessage}" +
                $"\n```\n" +
                $"Other Channel: <#{Match.GuildId}>")
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Gray)
                    .WithTitle("Partner Bot Advertisment")
                    .WithDescription($"**ID:** {Match.GuildId}")
                    .WithImageUrl(Match.Banner)
                );

            await rest.ExecuteWebhookAsync(WebhookId, WebhookToken, hook);
        }
    }
}
