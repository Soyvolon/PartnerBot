﻿using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DSharpPlus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PartnerBot.Core.Database;
using PartnerBot.Core.Database.Configuration;
using PartnerBot.Core.Entities.Configuration;
using PartnerBot.Core.Services;
using PartnerBot.Discord;
using PartnerBot.Discord.Services;

namespace PartnerBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Start(args).GetAwaiter().GetResult();
        }

        private static async Task Start(string[] args)
        {
            PartnerBotConfiguration pcfg;
            using (FileStream fs = new(Path.Join("Config", "bot_config.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                pcfg = await JsonSerializer.DeserializeAsync<PartnerBotConfiguration>(fs) ?? throw new Exception("Failed to parse bot config file");
            }

            PartnerDatabaseConfiguration dbConfig;
            using (FileStream fs = new(Path.Join("Config", "database_config.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                dbConfig = await JsonSerializer.DeserializeAsync<PartnerDatabaseConfiguration>(fs) ?? throw new Exception("Failed to read Partner Database config");
            }

            ServiceCollection services = new ServiceCollection();

            DiscordConfiguration botCfg = new()
            {
                Intents = DiscordIntents.Guilds
                            | DiscordIntents.GuildMessages
                            | DiscordIntents.GuildMessageReactions
                            | DiscordIntents.GuildIntegrations
                            | DiscordIntents.GuildInvites,
                TokenType = TokenType.Bot,
                MessageCacheSize = 0,
#if DEBUG
                MinimumLogLevel = LogLevel.Debug,
#else
                MinimumLogLevel = LogLevel.Information,
#endif
                Token = pcfg.Token
            };

            services.AddDbContext<PartnerDatabaseContext>(options =>
            {
                options.UseSqlite(dbConfig.PartnerbotDataSource);
            },
            ServiceLifetime.Scoped, ServiceLifetime.Scoped)
                .AddSingleton<DiscordShardedClient>((s) =>
                {
                    return new(botCfg);
                })
                .AddSingleton<DiscordRestClient>((s) =>
                {
                    return new(botCfg);
                })
                .AddSingleton<PartnerManagerService>()
                .AddSingleton<ChannelVerificationService>()
                .AddSingleton<PartnerSenderService>()
                .AddSingleton<ClientLoggingService>()
                .AddSingleton<CommandErrorHandlingService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<DiscordBot>()
                .AddSingleton(pcfg);

            IServiceProvider provider = services.BuildServiceProvider();

            var db = provider.GetRequiredService<PartnerDatabaseContext>();

            await ApplyDatabaseMigrations(db);

            var bot = provider.GetRequiredService<DiscordBot>();

            await bot.InitalizeAsync();

            await bot.StartAsync();

            await Task.Delay(-1);
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
