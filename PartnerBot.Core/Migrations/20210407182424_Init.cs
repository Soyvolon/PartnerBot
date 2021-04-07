using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PartnerBot.Core.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildBans",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    BanTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildBans", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "GuildConfigurations",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Prefix = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigurations", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildName = table.Column<string>(type: "TEXT", nullable: false),
                    GuildIcon = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    WebhookId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    DonorRank = table.Column<int>(type: "INTEGER", nullable: false),
                    Banner = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Invite = table.Column<string>(type: "TEXT", nullable: false),
                    NSFW = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReceiveNSFW = table.Column<bool>(type: "INTEGER", nullable: false),
                    WebhookToken = table.Column<string>(type: "TEXT", nullable: false),
                    UserCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LinksUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseColor = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageEmbeds = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.GuildId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildBans");

            migrationBuilder.DropTable(
                name: "GuildConfigurations");

            migrationBuilder.DropTable(
                name: "Partners");
        }
    }
}
