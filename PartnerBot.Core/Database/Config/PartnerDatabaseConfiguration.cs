using Newtonsoft.Json;

namespace PartnerBot.Core.Database.Config
{
    public class PartnerDatabaseConfiguration
    {
        [JsonProperty("partner_database_source")]
        public string DataSource { get; internal set; }
    }
}
