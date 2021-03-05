using System.IO;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;



using PartnerBot.Core.Database.Configuration;

namespace PartnerBot.Core.Database
{
    public class PartnerDatabaseFactory : IDesignTimeDbContextFactory<PartnerDatabaseContext>
    {
        public PartnerDatabaseContext CreateDbContext(string[] args)
        {
            PartnerDatabaseConfiguration dbConfig;
            using (FileStream fs = new(Path.Join("Config", "database_config.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                StreamReader sr = new(fs);
                var json = sr.ReadToEnd();

                dbConfig = JsonSerializer.Deserialize<PartnerDatabaseConfiguration>(json) ?? throw new System.Exception("Failed to read Partner Database config");
            }

            var optionsBuilder = new DbContextOptionsBuilder<PartnerDatabaseContext>();
            optionsBuilder.UseSqlite(dbConfig.PartnerbotDataSource);

            return new PartnerDatabaseContext(optionsBuilder.Options);
        }
    }
}
