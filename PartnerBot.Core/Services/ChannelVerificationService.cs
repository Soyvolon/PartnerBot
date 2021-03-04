using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DSharpPlus;

using Microsoft.EntityFrameworkCore;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;

namespace PartnerBot.Core.Services
{
    public class ChannelVerificationService
    {
        public const int DOUBLE_SECONDS_PER_DAY = 43200;

        // read and see the channels.
        public readonly Permissions RequiredPermissions = Permissions.AccessChannels | Permissions.ReadMessageHistory;

        public ConcurrentBag<int> SlotsBag { get; private set; }
        public ConcurrentDictionary<ulong, ulong>[] ChannelTree { get; init; }
        public ConcurrentDictionary<ulong, int> SlotTree { get; private set; }
        public Timer VerificationTimer { get; private set; }

        private readonly PartnerDatabaseContext _database;
        private readonly DiscordRestClient _rest;

        private int CurrentSlot { get; set; }

        public ChannelVerificationService(PartnerDatabaseContext database, DiscordRestClient rest)
        {
            _database = database;
            _rest = rest;

            ChannelTree = new ConcurrentDictionary<ulong, ulong>[DOUBLE_SECONDS_PER_DAY];
            SlotsBag = new();
            SlotTree = new();
        }

        public void Initalize()
        {
            SlotsBag = new();
            SlotTree = new();

            for (int i = 0; i < DOUBLE_SECONDS_PER_DAY; i++)
            {
                SlotsBag.Add(i);
                ChannelTree[i] = new();
            }

            var active = _database.Partners
                .AsNoTracking()
                .Where(x => x.Active);

            ConcurrentBag<Partner> bag = new(active);

            while(bag.TryTake(out var res))
            {
                if (SlotsBag.Count <= 0)
                    RegenerateSlotBag();

                if (SlotsBag.TryTake(out var slot))
                {
                    ChannelTree[slot][res.WebhookId] = res.GuildId;
                    SlotTree[res.GuildId] = slot;
                }
            }

            CurrentSlot = -1; // auto increments at start, so set init to -1;

            VerificationTimer = new(
                async (x) => await VerifyNextSlot(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2)
            );
        }

        internal void AddPartner(Partner p)
        {
            if (!SlotsBag.TryTake(out int slot))
            {
                RegenerateSlotBag();
                if (!SlotsBag.TryTake(out slot))
                    throw new Exception("Failed to regerneate slot bag.");
            }

            SlotTree[p.GuildId] = slot;
            ChannelTree[slot][p.WebhookId] = p.GuildId;
        }

        internal void RemovePartner(Partner p)
        {
            // Remove this partner from its slot, from the slot tree, and add the slot back to the slot bag

            if(SlotTree.TryRemove(p.GuildId, out var slot))
            {
                _ = ChannelTree[slot].TryRemove(p.WebhookId, out _);
                SlotsBag.Add(slot);
            }
        }

        private void RegenerateSlotBag()
        {
            for (int i = 0; i < DOUBLE_SECONDS_PER_DAY; i++)
                SlotsBag.Add(i);
        }

        private async Task VerifyNextSlot()
        {
            if (++CurrentSlot >= DOUBLE_SECONDS_PER_DAY)
                CurrentSlot = 0;

            var ids = ChannelTree[CurrentSlot];

            foreach(var id in ids)
            {
                try
                {
                    var hook = await _rest.GetWebhookAsync(id.Key);
                    var channel = await _rest.GetChannelAsync(hook.ChannelId);

                    foreach (var overwrite in channel.PermissionOverwrites)
                    {
                        if (overwrite.Denied.HasPermission(RequiredPermissions))
                        {
                            await DisablePartner(id.Value);
                        }
                    }
                }
                catch
                {
                    await DisablePartner(id.Value);
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.15));
                }
            }
        }

        private async Task DisablePartner(ulong guildId)
        {
            var p = await _database.FindAsync<Partner>(guildId);
            p.Active = false;
            await _database.SaveChangesAsync();

            RemovePartner(p);
        }
    }
}
