using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DSharpPlus.Entities;

using PartnerBot.Core.Utils;

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
        public bool IsSetup()
        {
            return !string.IsNullOrWhiteSpace(WebhookToken)
                && WebhookId != 0
                && !string.IsNullOrWhiteSpace(Invite);
        }

        public void ModifyToDonorRank()
        {
            RemoveExtraLinks();
        }

        private void RemoveExtraLinks()
        {
            int links = 0;

            var messageUrls = Message.GetUrls();

            foreach(var l in messageUrls)
            {
                if(++links > DonorRank)
                {
                    Message.Replace(l, string.Empty);
                }
            }

            foreach(var embed in MessageEmbeds)
            {
                var eMsgLinks = embed.Description.GetUrls();

                foreach (var l in eMsgLinks)
                {
                    if (++links > DonorRank)
                    {
                        embed.Description.Replace(l, string.Empty);
                    }
                }

                foreach(var field in embed.Fields)
                {
                    var fLinks = field.Value.GetUrls();

                    foreach (var l in fLinks)
                    {
                        if (++links > DonorRank)
                        {
                            field.Value.Replace(l, string.Empty);
                        }
                    }

                    var titleLinks = field.Name.GetUrls();

                    foreach (var l in titleLinks)
                    {
                        field.Name.Replace(l, string.Empty);
                    }
                }

                var tLinks = embed.Title.GetUrls();

                foreach (var l in tLinks)
                {
                    embed.Title.Replace(l, string.Empty);
                }
            }

            LinksUsed = links > DonorRank ? DonorRank : links;
        }
    }
}
