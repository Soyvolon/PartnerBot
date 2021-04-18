

using System.Text.Json.Serialization;

namespace PartnerBot.Core.Database.Configuration
{
    /// <summary>
    /// Holds the data source for the Partner Bot Database
    /// </summary>
    public class PartnerDatabaseConfiguration
    {
        [JsonPropertyName("partnerbot_data_source")]
        public string PartnerbotDataSource { get; internal set; }

        [JsonConstructor]
        public PartnerDatabaseConfiguration(string partnerbotDataSource)
        {
            this.PartnerbotDataSource = partnerbotDataSource;
        }
    }
}
