using System.Collections.Generic;

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
    }
}
