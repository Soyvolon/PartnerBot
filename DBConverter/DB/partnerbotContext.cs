using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DBConverter
{
    public partial class partnerbotContext : DbContext
    {
        public partnerbotContext()
        {
        }

        public partnerbotContext(DbContextOptions<partnerbotContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Ad> Ads { get; set; }
        public virtual DbSet<Guildban> Guildbans { get; set; }
        public virtual DbSet<Guildconfig> Guildconfigs { get; set; }
        public virtual DbSet<Partnerlist> Partnerlists { get; set; }
        public virtual DbSet<Testpartnerlist> Testpartnerlists { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseMySQL("Server=localhost;Port=3306;Database=partnerbot;Uid=root;Pwd=admin");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ad>(entity =>
            {
                entity.ToTable("ads");

                entity.Property(e => e.Message).HasColumnType("longtext");
            });

            modelBuilder.Entity<Guildban>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("guildbans");

                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasColumnType("longtext");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Name).HasMaxLength(50);

                entity.Property(e => e.Reason)
                    .IsRequired()
                    .HasColumnType("longtext");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Guildconfig>(entity =>
            {
                entity.HasKey(e => e.GuildId)
                    .HasName("PRIMARY");

                entity.ToTable("guildconfig");

                entity.Property(e => e.GuildId).HasColumnName("guildID");

                entity.Property(e => e.LogChannel)
                    .HasColumnType("longtext")
                    .HasColumnName("logChannel");

                entity.Property(e => e.LogEnabled)
                    .HasColumnType("longtext")
                    .HasColumnName("logEnabled");

                entity.Property(e => e.Managers)
                    .HasColumnType("longtext")
                    .HasColumnName("managers");

                entity.Property(e => e.Prefix)
                    .HasColumnType("longtext")
                    .HasColumnName("prefix");
            });

            modelBuilder.Entity<Partnerlist>(entity =>
            {
                entity.HasKey(e => e.GuildId)
                    .HasName("PRIMARY");

                entity.ToTable("partnerlist");

                entity.Property(e => e.Data).HasColumnType("mediumtext");

                entity.Property(e => e.GuildName).HasColumnType("longtext");

                entity.Property(e => e.Message).HasColumnType("mediumtext");

                entity.Property(e => e.Nsfw)
                    .HasColumnName("NSFW")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.ReceiveNsfw)
                    .HasColumnName("ReceiveNSFW")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Tags).HasColumnType("mediumtext");
            });

            modelBuilder.Entity<Testpartnerlist>(entity =>
            {
                entity.HasKey(e => e.GuildId)
                    .HasName("PRIMARY");

                entity.ToTable("testpartnerlist");

                entity.Property(e => e.Banner).HasColumnType("longtext");

                entity.Property(e => e.Data).HasColumnType("longtext");

                entity.Property(e => e.Message).HasColumnType("longtext");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
