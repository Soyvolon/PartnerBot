﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;

namespace PartnerBot.Core.Entities
{
    public class PartnerData : Partner
    {
        public Partner Match { get; internal set; }
        public bool ExtraMessage { get; internal set; }

        public async Task ExecuteAsync(DiscordRestClient rest, PartnerSenderArguments senderArguments)
        {
            if (senderArguments.DevelopmentStressTest)
            {
                await ExecuteStressTestMessage(rest);
                return;
            }

            var hook = new DiscordWebhookBuilder()
                .WithContent($"{Match.Message}\n\n" +
                $"https://discord.gg/{Match.Invite}")
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Gray)
                    .WithTitle("Partner Bot Advertisment")
                    .WithDescription($"**ID:** {Match.GuildId}")
                    .WithImageUrl(Match.Banner))
                .WithUsername($"{Match.GuildName} | Partner Bot");

            if (!string.IsNullOrWhiteSpace(Match.GuildIcon))
                hook.WithAvatarUrl(Match.GuildIcon);

            await rest.ExecuteWebhookAsync(WebhookId, WebhookToken, hook);
        }

        private async Task ExecuteStressTestMessage(DiscordRestClient rest)
        {
            var hook = new DiscordWebhookBuilder()
                .WithContent("```\n" +
                "STRESS TEST\n\n" +
                "This Partner:\n" +
                $"ID: {GuildId}\n" +
                $"Tags: {string.Join(", ", GetTags())}\n" +
                $"Size: {UserCount}\n\n" +
                $"Other Partner:" +
                $"ID: {Match.GuildId}\n" +
                $"Tags: {string.Join(", ", Match.GetTags())}\n" +
                $"Size: {Match.UserCount}" +
                $"\n```")
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
