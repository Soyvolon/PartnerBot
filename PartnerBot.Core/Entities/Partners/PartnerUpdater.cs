using System.Collections.Generic;

using DSharpPlus.Entities;

namespace PartnerBot.Core.Entities
{
    public class PartnerUpdater
    {
        public string? GuildName { get; set; } = null;
        public string? GuildIcon { get; set; } = null;
        public ulong? OwnerId { get; set; } = null;
        public string? Message { get; set; } = null;
        public bool? Active { get; set; } = null;
        public int? DonorRank { get; set; } = null;
        public string? Banner { get; set; } = null;
        public HashSet<string> TagsToAdd { get; set; } = new();
        public HashSet<string> TagsToRemove { get; set; } = new();
        public HashSet<string>? TagOverride { get; set; } = null;
        public string? Invite { get; set; } = null;
        public bool? NSFW { get; set; } = null;
        public bool? ReceiveNSFW { get; set; } = null;
        public string? WebhookToken { get; set; } = null;
        public ulong? ChannelId { get; set; } = null;
        public int? UserCount { get; set; } = null;
        public int? LinksUsed { get; set; } = null;
        public DiscordColor? BaseColor { get; set; } = null;
        public List<DiscordEmbedBuilder>? MessageEmbeds { get; set; } = null;

        public static PartnerUpdater BuildFromPartner(Partner p)
        {
            return new PartnerUpdater()
            {
                OwnerId = p.OwnerId,
                Message = p.Message,
                Active = p.Active,
                DonorRank = p.DonorRank,
                Banner = p.Banner,
                Invite = p.Invite,
                NSFW = p.NSFW,
                ReceiveNSFW = p.ReceiveNSFW,
                WebhookToken = p.WebhookToken,
                UserCount = p.UserCount,
                LinksUsed = p.LinksUsed,
                BaseColor = p.BaseColor,
                MessageEmbeds = p.MessageEmbeds,
                GuildName = p.GuildName,
                GuildIcon = p.GuildIcon
            };
        }
    }
}
