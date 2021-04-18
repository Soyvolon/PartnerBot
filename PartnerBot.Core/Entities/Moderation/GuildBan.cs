using System;
using System.ComponentModel.DataAnnotations;

namespace PartnerBot.Core.Entities.Moderation
{
    /// <summary>
    /// Stores data about a Partner Bot ban.
    /// </summary>
    public class GuildBan
    {
        [Key]
        public ulong GuildId { get; set; }
        public string? Reason { get; set; }
        public DateTime BanTime { get; set; }
    }
}
