using System.IO;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;



using PartnerBot.Core.Database.Config;

namespace PartnerBot.Core.Database
{
    public class PartnerDatabaseFactory : IDesignTimeDbContextFactory<PartnerDatabaseContext>
    {
        public PartnerDatabaseContext CreateDbContext(string[] args)
        {
            PartnerDatabaseConfiguration dbConfig;
            using (FileStream fs = new(Path.Join("Config", "database_config.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using StreamReader sr = new(fs);
                var json = sr.ReadToEnd();
                var configDoc = JsonDocument.Parse(json);

                configDoc.RootElement.TryGetProperty("partner_database_source", out var source);
                
                dbConfig = new()
                {
                    DataSource = source.ToString() ?? ""
                };
            }

            var optionsBuilder = new DbContextOptionsBuilder<PartnerDatabaseContext>();
            optionsBuilder.UseSqlite(dbConfig.DataSource);

            return new PartnerDatabaseContext(optionsBuilder.Options);
        }
    }
}
