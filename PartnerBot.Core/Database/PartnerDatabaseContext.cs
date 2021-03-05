
using Microsoft.EntityFrameworkCore;

using PartnerBot.Core.Entities;
using PartnerBot.Core.Entities.Configuration;

namespace PartnerBot.Core.Database
{
    public class PartnerDatabaseContext : DbContext
    {
        public DbSet<Partner> Partners { get; protected set; }
        public DbSet<DiscordGuildConfiguration> GuildConfigurations { get; protected set; }

        public PartnerDatabaseContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            base.OnModelCreating(modelBuilder);
        }
    }
}
