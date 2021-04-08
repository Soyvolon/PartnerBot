using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PartnerBot.Core.Database;
using PartnerBot.Core.Database.Configuration;

namespace DBConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            ServiceCollection services = new();
            services.AddLogging(o => o.SetMinimumLevel(LogLevel.Information).AddConsole())
                .AddSingleton<IConfiguration>((x) =>
                {
                    return new ConfigurationBuilder()
                        .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
#if DEBUG
                        .AddJsonFile("appsettings.Development.json", false)
#else
                        .AddJsonFile("appsettings.json", false)
#endif
                        .Build();
                });

            var provider = services.BuildServiceProvider();

            var logger = provider.GetRequiredService<ILogger<Program>>();
            var config = provider.GetRequiredService<IConfiguration>();

            try
            {
                logger.LogInformation("Registering database services...");

                services.AddDbContext<partnerbotContext>(options =>
                    {
                        var cstring = $"Server={config["Database:Remote:Host"]};" +
                            $"Port={config["Database:Remote:Port"]};" +
                            $"Database={config["Database:Remote:Database"]};" +
                            $"Uid={config["Database:Remote:Username"]};" +
                            $"Pwd={config["Database:Remote:Password"]}";

                        options.UseMySQL(cstring);
                    })
                    .AddDbContext<PartnerDatabaseContext>(options =>
                    {
                        options.UseSqlite(config["Database:Local:DataSource"]);
                    });

                provider = services.BuildServiceProvider();

                logger.LogInformation("Starting data transfer...");

                var remote = provider.GetRequiredService<partnerbotContext>();

                var local = provider.GetRequiredService<PartnerDatabaseContext>();
                await ApplyDatabaseMigrations(local);

                HashSet<ulong> added = new();
                await remote.Partnerlists.ForEachAsync(async (x) =>
                {
                    var uid = (ulong)x.GuildId;
                    if (!added.Add(uid)) return;

                    await local.Partners.AddAsync(new()
                    {
                        Active = false,
                        Banner = x.Banner,
                        DonorRank = x.DonorRank.HasValue ? x.DonorRank.Value - 1 : 0,
                        GuildId = uid,
                        Message = x.Message.Length <= 2 ? "" : x.Message[1..(x.Message.Length-2)],
                        NSFW = Convert.ToBoolean(x.Nsfw ?? 0),
                        ReceiveNSFW = Convert.ToBoolean(x.ReceiveNsfw ?? 0),
                        OwnerId = (ulong)x.OwnerId,
                        GuildName = string.IsNullOrWhiteSpace(x.GuildName) ? "n/a" : x.GuildName
                    });

                    _ = Task.Run(() => logger.LogDebug($"Parsed Partner Data for {x.GuildName} : {x.GuildId}"));
                });

                await local.SaveChangesAsync();

                logger.LogInformation("\n\nSaved Partner Data\n\n");

                added = new();
                await remote.Guildbans.ForEachAsync(async (x) =>
                {
                    var id = (ulong)x.Id;
                    if (!added.Add(id)) return;

                    await local.GuildBans.AddAsync(new()
                    {
                        BanTime = DateTime.Now,
                        GuildId = id,
                        Reason = x.Reason
                    });
                });

                await local.SaveChangesAsync();
                
                logger.LogInformation("\n\nSaved Guild Bans Data\n\n");

                added = new();
                await remote.Guildconfigs.ForEachAsync(async (x) =>
                {
                    var gid = (ulong)x.GuildId;
                    if (!added.Add(gid)) return;

                    await local.GuildConfigurations.AddAsync(new()
                    {
                        GuildId = gid,
                        Prefix = string.IsNullOrWhiteSpace(x.Prefix) ? "pb!" : x.Prefix
                    });

                    _ = Task.Run(() => logger.LogDebug($"Parsed Guild Config for {x.GuildId}"));
                });

                await local.SaveChangesAsync();

                logger.LogInformation("\n\nSaved Guild Config Information\n\n");



                logger.LogInformation("Operation Completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occoured.");
            }
        }

        private static async Task ApplyDatabaseMigrations(DbContext database)
        {
            if (!(await database.Database.GetPendingMigrationsAsync()).Any())
            {
                return;
            }

            await database.Database.MigrateAsync();
            await database.SaveChangesAsync();
        }
    }
}
