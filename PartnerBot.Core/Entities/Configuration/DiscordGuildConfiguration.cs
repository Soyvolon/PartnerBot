using System.ComponentModel.DataAnnotations;

namespace PartnerBot.Core.Entities.Configuration
{
    /// <summary>
    /// Stores saved data about a guild.
    /// </summary>
    public class DiscordGuildConfiguration
    {
        [Key]
        public ulong GuildId { get; set; }
        public string Prefix { get; set; }

        public DiscordGuildConfiguration()
        {
            this.GuildId = 0;
            this.Prefix = "-^-";
        }
    }
}
