

using System.Text.Json.Serialization;

namespace PartnerBot.Core.Database.Configuration
{
    public class PartnerDatabaseConfiguration
    {
        [JsonPropertyName("partnerbot_data_source")]
        public string PartnerbotDataSource { get; internal set; }

        [JsonConstructor]
        public PartnerDatabaseConfiguration(string partnerbotDataSource)
        {
            PartnerbotDataSource = partnerbotDataSource;
        }
    }
}
