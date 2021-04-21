using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;

namespace PartnerBot.Core.Services
{
    /// <summary>
    /// The service responsible for ensuring channels are valid while the bot is in use.
    /// </summary>
    public class GuildVerificationService
    {
        public const int DOUBLE_SECONDS_PER_DAY = 43200;

        // Read and see the channels.
        public static readonly Permissions RequiredPermissions = Permissions.AccessChannels | Permissions.ReadMessageHistory;
        private static readonly EventId _event = new EventId(12701, "GuildVerify");

        public ConcurrentBag<int> SlotsBag { get; private set; }
        public ConcurrentDictionary<ulong, ulong>[] ChannelTree { get; init; }
        public ConcurrentDictionary<ulong, int> SlotTree { get; private set; }
        public Timer? VerificationTimer { get; private set; }

        private readonly IServiceProvider _services;
        private readonly DiscordRestClient _rest;
        private readonly DonorService _donor;
        private readonly ILogger _logger;

        private int CurrentSlot { get; set; }

        public GuildVerificationService(IServiceProvider services, DiscordRestClient rest,
            DonorService donor)
        {
            this._services = services;
            this._rest = rest;
            this._donor = donor;
            this._logger = rest.Logger;

            this.ChannelTree = new ConcurrentDictionary<ulong, ulong>[DOUBLE_SECONDS_PER_DAY];
            this.SlotsBag = new();
            this.SlotTree = new();
        }

        public async Task InitalizeAsync()
        {
            this.SlotsBag = new();
            this.SlotTree = new();
            this.VerificationTimer = null;

            for (int i = DOUBLE_SECONDS_PER_DAY - 1; i >= 0; i--)
            {
                this.SlotsBag.Add(i);
                this.ChannelTree[i] = new();
            }

            PartnerDatabaseContext? _database = this._services.GetRequiredService<PartnerDatabaseContext>();

            ConcurrentBag<Partner> bag = new();

            await _database.Partners.AsNoTracking().ForEachAsync(x =>
            {
                if (x.Active)
                    bag.Add(x);
            });

            while(bag.TryTake(out Partner? res))
            {
                if (this.SlotsBag.Count <= 0)
                    RegenerateSlotBag();

                if (this.SlotsBag.TryTake(out int slot))
                {
                    _logger.LogDebug(_event, $"Added guild {res.GuildId} to slot {slot}");
                    this.ChannelTree[slot][res.WebhookId] = res.GuildId;
                    this.SlotTree[res.GuildId] = slot;
                }
            }

            // Auto increments at start, so set init to -1.
            this.CurrentSlot = -1; 
        }

        public void Start()
        {
            this.VerificationTimer = new(
                async (x) => await VerifyNextSlot(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2)
            );
        }

        internal void AddPartner(Partner p)
        {
            if (!this.SlotsBag.TryTake(out int slot))
            {
                RegenerateSlotBag();
                if (!this.SlotsBag.TryTake(out slot))
                    throw new Exception("Failed to regerneate slot bag.");
            }

            _logger.LogDebug(_event, $"Added guild {p.GuildId} to slot {slot}");

            this.SlotTree[p.GuildId] = slot;
            this.ChannelTree[slot][p.WebhookId] = p.GuildId;
        }

        internal void RemovePartner(Partner p)
        {
            // Remove this partner from its slot, from the slot tree, and add the slot back to the slot bag.

            if(this.SlotTree.TryRemove(p.GuildId, out int slot))
            {
                _ = this.ChannelTree[slot].TryRemove(p.WebhookId, out _);
                this.SlotsBag.Add(slot);
            }
        }

        private void RegenerateSlotBag()
        {
            //for (int i = 0; i < DOUBLE_SECONDS_PER_DAY; i++)
            for (int i = DOUBLE_SECONDS_PER_DAY - 1; i >= 0; i--)
                this.SlotsBag.Add(i);
        }

        private async Task VerifyNextSlot()
        {
            if (++this.CurrentSlot >= DOUBLE_SECONDS_PER_DAY)
                this.CurrentSlot = 0;

            _logger.LogTrace(_event, $"Starting verifcation for slot {this.CurrentSlot}");

            ConcurrentDictionary<ulong, ulong>? ids = this.ChannelTree[this.CurrentSlot];

            _logger.LogTrace(_event, $"Found {ids.Count} IDs to verify.");

            foreach (System.Collections.Generic.KeyValuePair<ulong, ulong> id in ids)
            {
                _logger.LogInformation(_event, $"Starting verification for guild {id.Value}");
                try
                {
                    DiscordWebhook? hook = await this._rest.GetWebhookAsync(id.Key);
                    DiscordChannel? channel = await this._rest.GetChannelAsync(hook.ChannelId);

                    if(!VerifyChannel(channel))
                    {
                        _logger.LogWarning(_event, $"Invalid setup detected for guild {id.Value}");
                        await DisablePartner(id.Value);
                        _ = Task.Run(async () => await hook.ExecuteAsync(new DiscordWebhookBuilder()
                            .WithContent("Your server has been disabled due to an invalid setup. Please re-do your" +
                            " Partern Setup.")));
                        continue;
                    }

                    await UpdateDonorRanking(hook.GuildId);
                }
                catch
                {
                    _logger.LogWarning(_event, $"Invalid setup detected for guild {id.Value}");
                    await DisablePartner(id.Value);
                    continue;
                }

                DiscordGuild? guild = await this._rest.GetGuildAsync(id.Value);

                PartnerDatabaseContext? _database = this._services.GetRequiredService<PartnerDatabaseContext>();
                Partner? p = await _database.FindAsync<Partner>(id.Value);
                p.UserCount = guild.MemberCount;
                await _database.SaveChangesAsync();
            }
        }

        private async Task DisablePartner(ulong guildId)
        {
            PartnerDatabaseContext? _database = this._services.GetRequiredService<PartnerDatabaseContext>();
            Partner? p = await _database.FindAsync<Partner>(guildId);
            p.Active = false;
            await _database.SaveChangesAsync();

            RemovePartner(p);
        }

        public static bool VerifyChannel(DiscordChannel c)
        {
            foreach (DiscordOverwrite? overwrite in c.PermissionOverwrites)
            {
                if (!overwrite.Allowed.HasPermission(RequiredPermissions))
                {
                    return false;
                }
            }

            return true;
        }

        public async Task UpdateDonorRanking(ulong guildId)
        {
            PartnerDatabaseContext? _database = this._services.GetRequiredService<PartnerDatabaseContext>();
            Partner? p = await _database.FindAsync<Partner>(guildId);

            int rank = await this._donor.GetDonorRankAsync(p.OwnerId);

            p.DonorRank = rank;
            await _database.SaveChangesAsync();
        }
    }
}
