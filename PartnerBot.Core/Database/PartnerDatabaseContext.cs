
using System.Collections.Generic;

using DSharpPlus.Entities;

using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;

using PartnerBot.Core.Entities;
using PartnerBot.Core.Entities.Configuration;
using PartnerBot.Core.Entities.Moderation;

namespace PartnerBot.Core.Database
{
    public class PartnerDatabaseContext : DbContext
    {
        public DbSet<Partner> Partners { get; protected set; }
        public DbSet<DiscordGuildConfiguration> GuildConfigurations { get; protected set; }
        public DbSet<GuildBan> GuildBans { get; protected set; }

        public PartnerDatabaseContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Parse invalid property values into database saveable values and back.
            Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Partner>? partner = modelBuilder.Entity<Partner>();
            partner.HasKey(x => x.GuildId);
            partner.Property(x => x.BaseColor)
                .HasConversion(
                    v => v.Value,
                    v => new(v));
            partner.Property(x => x.MessageEmbeds)
                .HasConversion( 
                // Use newtonsoft.json here because DSharpPlus uses it to define the embed.
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<List<DiscordEmbed>>(v) ?? new());
            partner.Property(x => x.Tags)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, null),
                    v => System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(v, null) ?? new());

            base.OnModelCreating(modelBuilder);
        }
    }
}
