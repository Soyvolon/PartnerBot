using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DSharpPlus.Entities;

namespace PartnerBot.Core.Entities
{
    public class Partner
    {
        public ulong GuildId { get; set; } = 0;
        public string GuildName { get; set; } = "";
        public string GuildIcon { get; set; } = "";
        public ulong OwnerId { get; set; } = 0;
        public ulong WebhookId { get; set; } = 0;
        public string Message { get; set; } = "";
        public bool Active { get; set; } = false;
        public int DonorRank { get; set; } = 0;
        public string Banner { get; set; } = "";
        internal HashSet<string> Tags { get; private set; } = new();
        public string Invite { get; set; } = "";
        public bool NSFW { get; set; } = false;
        public bool ReceiveNSFW { get; set; } = false;
        public string WebhookToken { get; internal set; } = "";
        public int UserCount { get; set; } = -1;
        public int LinksUsed { get; set; } = 0;
        public DiscordColor BaseColor { get; set; } = DiscordColor.Gray;
        public List<DiscordEmbedBuilder> MessageEmbeds { get; set; } = new();

        public Partner() { }

        public PartnerData BuildData(Partner match, bool extra)
        {
            var data = new PartnerData(this, match, extra);

            return data;
        }

        /// <summary>
        /// Gets a rough idea if the bot has been setup before or not.
        /// </summary>
        /// <returns>True if the bot has been setup before.</returns>
        public bool IsSetup()
        {
            return !string.IsNullOrWhiteSpace(WebhookToken)
                && WebhookId != 0
                && !string.IsNullOrWhiteSpace(Invite);
        }
    }
}
