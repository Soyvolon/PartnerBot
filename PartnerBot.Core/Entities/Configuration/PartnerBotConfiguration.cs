using System.Text.Json.Serialization;

namespace PartnerBot.Core.Entities.Configuration
{
    public class PartnerBotConfiguration
    {
        [JsonPropertyName("token")]
        public string Token { get; internal set; }
        [JsonPropertyName("prefix")]
        public string Prefix { get; internal set; }

        [JsonConstructor]
        public PartnerBotConfiguration(string token, string prefix)
        {
            Token = token;
            Prefix = prefix;
        }
    }
}
