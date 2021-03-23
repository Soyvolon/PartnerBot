﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PartnerBot.Core.Database;

namespace PartnerBot.Core.Migrations
{
    [DbContext(typeof(PartnerDatabaseContext))]
    partial class PartnerDatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.3");

            modelBuilder.Entity("PartnerBot.Core.Entities.Configuration.DiscordGuildConfiguration", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Prefix")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId");

                    b.ToTable("GuildConfigurations");
                });

            modelBuilder.Entity("PartnerBot.Core.Entities.Partner", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Active")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Banner")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("BaseColor")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DonorRank")
                        .HasColumnType("INTEGER");

                    b.Property<string>("GuildIcon")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("GuildName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Invite")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("LinksUsed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MessageEmbeds")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("NSFW")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("OwnerId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ReceiveNSFW")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Tags")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("UserCount")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("WebhookId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("WebhookToken")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId");

                    b.ToTable("Partners");
                });
#pragma warning restore 612, 618
        }
    }
}
