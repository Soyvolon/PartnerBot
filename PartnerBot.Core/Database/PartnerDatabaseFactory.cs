using System.IO;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using Newtonsoft.Json;

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
                dbConfig = JsonConvert.DeserializeObject<PartnerDatabaseConfiguration>(json);
            }

            var optionsBuilder = new DbContextOptionsBuilder<PartnerDatabaseContext>();
            optionsBuilder.UseSqlite(dbConfig.DataSource);

            return new PartnerDatabaseContext(optionsBuilder.Options);
        }
    }
}
