#nullable disable

namespace DBConverter
{
    public partial class Testpartnerlist
    {
        public long GuildId { get; set; }
        public string Data { get; set; }
        public long? OwnerId { get; set; }
        public long? ChannelId { get; set; }
        public string Message { get; set; }
        public bool? Active { get; set; }
        public int? DonorRank { get; set; }
        public string Banner { get; set; }
    }
}
