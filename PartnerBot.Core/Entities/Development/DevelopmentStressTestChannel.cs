﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PartnerBot.Core.Entities
{
    /// <summary>
    /// Data for a development stress test channel.
    /// </summary>
    public class DevelopmentStressTestChannel
    {
        [JsonPropertyName("channel_id")]
        public ulong ChannelId { get; set; }
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("guild_id")]
        public ulong GuildId { get; set; }
        [JsonPropertyName("guild_name")]
        public string GuildName { get; set; }
        [JsonPropertyName("webhook_id")]
        public ulong WebhookId { get; set; }
        [JsonPropertyName("webhook_token")]
        public string WebhookToken { get; set; }
    }
}
