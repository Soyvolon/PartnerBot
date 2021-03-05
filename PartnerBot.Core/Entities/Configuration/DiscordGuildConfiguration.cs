using System.ComponentModel.DataAnnotations;

namespace PartnerBot.Core.Entities.Configuration
{
    public class DiscordGuildConfiguration
    {
        [Key]
        public ulong GuildId { get; set; }
        public string Prefix { get; set; }

        public DiscordGuildConfiguration()
        {
            GuildId = 0;
            Prefix = "-^-";
        }
    }
}
