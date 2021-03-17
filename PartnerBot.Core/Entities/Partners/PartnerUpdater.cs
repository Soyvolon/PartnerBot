using System.Collections.Generic;

using DSharpPlus.Entities;

namespace PartnerBot.Core.Entities
{
    public class PartnerUpdater
    {
        public ulong? OwnerId { get; set; } = null;
        public string? Message { get; set; } = null;
        public bool? Active { get; set; } = null;
        public int? DonorRank { get; set; } = null;
        public string? Banner { get; set; } = null;
        public HashSet<string>? Tags { get; set; } = null;
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
                Tags = p.GetTags(),
                Invite = p.Invite,
                NSFW = p.NSFW,
                ReceiveNSFW = p.ReceiveNSFW,
                WebhookToken = p.WebhookToken,
                UserCount = p.UserCount,
                LinksUsed = p.UserCount,
                BaseColor = p.BaseColor,
                MessageEmbeds = p.MessageEmbeds
            };
        }
    }
}
