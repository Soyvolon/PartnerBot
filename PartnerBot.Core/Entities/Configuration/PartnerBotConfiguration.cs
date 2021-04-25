using System.Collections.Generic;
using System.Text.Json.Serialization;

using DSharpPlus;

namespace PartnerBot.Core.Entities.Configuration
{
    /// <summary>
    /// Stores the configuration data for Partner Bot
    /// </summary>
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
        [JsonPropertyName("donor_roles")]
        public List<PartnerBotDonorRoleConfiguration> DonorRoles { get; internal set; }
        [JsonPropertyName("bot_permissions")]
        public Permissions BotPermissions { get; internal set; }
        [JsonPropertyName("home_guild_invite")]
        public string InviteCode { get; internal set; }
        [JsonPropertyName("shard_count")]
        public int ShardCount { get; internal set; }

        [JsonConstructor]
        public PartnerBotConfiguration(string token, string prefix, List<ulong> owners, List<ulong> staffRoles, ulong homeGuild, List<PartnerBotDonorRoleConfiguration> donorRoles,
            Permissions botPermissions, string inviteCode, int shardCount)
        {
            this.Token = token;
            this.Prefix = prefix;
            this.Owners = owners;
            this.StaffRoles = staffRoles;
            this.HomeGuild = homeGuild;
            this.DonorRoles = donorRoles;
            this.BotPermissions = botPermissions;
            this.InviteCode = inviteCode;
            this.ShardCount = shardCount;
        }
    }

    public class PartnerBotDonorRoleConfiguration
    {
        [JsonPropertyName("role_id")]
        public ulong RoleId { get; internal set; }
        [JsonPropertyName("weight")]
        public short Weight { get; internal set; }

        [JsonConstructor]
        public PartnerBotDonorRoleConfiguration(ulong roleId, short weight)
        {
            this.RoleId = roleId;
            this.Weight = weight;
        }
    }
}
