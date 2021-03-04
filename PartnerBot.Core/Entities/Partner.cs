using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PartnerBot.Core.Entities
{
    public class Partner
    {
        [Key]
        public ulong GuildId { get; set; } = 0;
        public ulong OwnerId { get; set; } = 0;
        public ulong ChannelId { get; set; } = 0;
        public string Message { get; set; } = "";
        public bool Active { get; set; } = false;
        public int DonorRank { get; set; } = 0;
        public string Banner { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public string Invite { get; set; } = "";
        public bool NSFW { get; set; } = false;
        public bool ReceiveNSFW { get; set; } = false;
        public string WebhookToken { get; set; } = "";

        public Partner() { }

        public PartnerData? BuildData(Partner match, bool extra)
        {
            var data = (PartnerData)this;

            data.Match = match;
            data.ExtraMessage = extra;

            return data;
        }
    }
}
