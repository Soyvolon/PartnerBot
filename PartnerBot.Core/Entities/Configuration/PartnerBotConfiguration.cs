using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PartnerBot.Core.Entities.Configuration
{
    public class PartnerBotConfiguration
    {
        [JsonPropertyName("token")]
        public string Token { get; internal set; }
        [JsonPropertyName("prefix")]
        public string Prefix { get; internal set; }
        [JsonPropertyName("owners")]
        public List<ulong> Owners { get; internal set; }
        [JsonPropertyName("staff_roles")]
        public List<ulong> StaffRoles { get; internal set; }
        [JsonPropertyName("home_guild")]
        public ulong HomeGuild { get; internal set; }

        [JsonConstructor]
        public PartnerBotConfiguration(string token, string prefix, List<ulong> owners, List<ulong> staffRoles, ulong homeGuild)
        {
            Token = token;
            Prefix = prefix;
            Owners = owners;
            StaffRoles = staffRoles;
            HomeGuild = homeGuild;
        }
    }
}
