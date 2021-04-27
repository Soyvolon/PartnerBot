using System.Collections.Generic;

using DSharpPlus.Entities;

using PartnerBot.Core.Utils;

namespace PartnerBot.Core.Entities
{
    /// <summary>
    /// The core class for a Partner
    /// </summary>
    public class Partner
    {
        public ulong GuildId { get; set; } = 0;
        public string GuildName { get; set; } = "";
        public string? GuildIcon { get; set; } = "";
        public ulong OwnerId { get; set; } = 0;
        public ulong WebhookId { get; set; } = 0;
        public string Message { get; set; } = "";
        public bool Active { get; set; } = false;
        public int DonorRank { get; set; } = 0;
        public string Banner { get; set; } = "";
        public HashSet<string> Tags { get; set; } = new();
        public string Invite { get; set; } = "";
        public bool NSFW { get; set; } = false;
        public bool ReceiveNSFW { get; set; } = false;
        public string WebhookToken { get; set; } = "";
        public int UserCount { get; set; } = -1;
        public int LinksUsed { get; set; } = 0;
        public DiscordColor BaseColor { get; set; } = DiscordColor.Gray;
        public List<DiscordEmbed> MessageEmbeds { get; set; } = new();
        public string? VanityInvite { get; set; } = null;

        public Partner() { }

        public PartnerData BuildData(Partner match, Partner? extra)
        {
            var data = new PartnerData(this, match, extra);

            return data;
        }

        /// <summary>
        /// Gets a rough idea if the bot has been setup before or not.
        /// </summary>
        /// <returns>True if the bot has been setup before.</returns>
        public (bool, string) IsSetup()
        {
            if(string.IsNullOrWhiteSpace(this.WebhookToken) || this.WebhookId == 0)
                return (false, "Webhook is missing or invalid.");

            if (string.IsNullOrWhiteSpace(this.Invite))
                return (false, "Invite is missing or invalid.");

            if (Message.Length > 1900)
                return (false, "Message exceedes 1900 characters .");

            return (true, "");
        }

        public void ModifyToDonorRank()
        {
            RemoveExtraLinks();
        }

        private void RemoveExtraLinks()
        {
            int links = 0;

            IReadOnlyList<string>? messageUrls = this.Message.GetUrls();

            foreach(string? l in messageUrls)
            {
                if(++links > this.DonorRank)
                {
                    this.Message = this.Message.Replace(l, string.Empty);
                }
            }

            var newEmbeds = new List<DiscordEmbedBuilder>();
            foreach(DiscordEmbed? embed in this.MessageEmbeds)
            {
                IReadOnlyList<string>? eMsgLinks = embed.Description.GetUrls();

                var newEmbed = new DiscordEmbedBuilder(embed);

                foreach (string? l in eMsgLinks)
                {
                    if (++links > this.DonorRank)
                    {
                        newEmbed.Description = embed.Description.Replace(l, string.Empty);
                    }
                }

                foreach(DiscordEmbedField? field in newEmbed.Fields)
                {
                    IReadOnlyList<string>? fLinks = field.Value.GetUrls();

                    foreach (string? l in fLinks)
                    {
                        if (++links > this.DonorRank)
                        {
                            field.Value = field.Value.Replace(l, string.Empty);
                        }
                    }

                    IReadOnlyList<string>? titleLinks = field.Name.GetUrls();

                    foreach (string? l in titleLinks)
                    {
                        field.Name = field.Name.Replace(l, string.Empty);
                    }
                }

                IReadOnlyList<string>? tLinks = embed.Title.GetUrls();

                foreach (string? l in tLinks)
                {
                    newEmbed.Title = embed.Title.Replace(l, string.Empty);
                }

                newEmbeds.Add(newEmbed);
            }

            this.MessageEmbeds.Clear();
            foreach (var e in newEmbeds)
                this.MessageEmbeds.Add(e.Build());

            this.LinksUsed = links > this.DonorRank ? this.DonorRank : links;
        }
    }
}
