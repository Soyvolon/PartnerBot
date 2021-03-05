﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PartnerBot.Core.Entities
{
    public class Partner
    {
        [Key]
        public ulong GuildId { get; set; } = 0;
        public string GuildName { get; set; } = "";
        public string GuildIcon { get; set; } = "";
        public ulong OwnerId { get; set; } = 0;
        public ulong WebhookId { get; set; } = 0;
        public string Message { get; set; } = "";
        public bool Active { get; set; } = false;
        public int DonorRank { get; set; } = 0;
        public string Banner { get; set; } = "";
        [NotMapped]
        private HashSet<string>? _tagSet { get; set; } = null;
        public string TagString { get; private set; }
        public string Invite { get; set; } = "";
        public bool NSFW { get; set; } = false;
        public bool ReceiveNSFW { get; set; } = false;
        public string WebhookToken { get; internal set; } = "";
        public int UserCount { get; internal set; } = -1;

        public Partner() { }

        public PartnerData BuildData(Partner match, bool extra)
        {
            var data = (PartnerData)this;

            data.Match = match;
            data.ExtraMessage = extra;

            return data;
        }

        public HashSet<string> GetTags()
        {
            if (_tagSet is null)
                _tagSet = new(TagString.Split(",", System.StringSplitOptions.RemoveEmptyEntries));

            return _tagSet;
        }

        public void SetTags(HashSet<string> tags)
        {
            _tagSet = tags;
            TagString = string.Join(",", _tagSet);
        }

        public void SetTags(IList<string> tags)
        {
            _tagSet = new(tags);
            TagString = string.Join(",", _tagSet);
        }
    }
}
