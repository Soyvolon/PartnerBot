#nullable disable

namespace DBConverter
{
    public partial class Partnerlist
    {
        public long GuildId { get; set; }
        public string Data { get; set; }
        public long? OwnerId { get; set; }
        public long? ChannelId { get; set; }
        public string Message { get; set; }
        public byte? Active { get; set; }
        public int? DonorRank { get; set; }
        public string Banner { get; set; }
        public string Tags { get; set; }
        public string Invite { get; set; }
        public long? WebhookId { get; set; }
        public string GuildName { get; set; }
        public int? Nsfw { get; set; }
        public int? ReceiveNsfw { get; set; }
    }
}
