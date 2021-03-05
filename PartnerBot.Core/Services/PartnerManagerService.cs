using System;
using System.Threading.Tasks;

using DSharpPlus;

using Microsoft.Extensions.Logging;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;

namespace PartnerBot.Core.Services
{
    public class PartnerManagerService
    {
        private readonly ChannelVerificationService _channelVerification;
        private readonly PartnerDatabaseContext _database;
        private readonly DiscordRestClient _rest;

        public PartnerManagerService(ChannelVerificationService channelVerification, PartnerDatabaseContext database,
            DiscordRestClient rest)
        {
            _channelVerification = channelVerification;
            _database = database;
            _rest = rest;
        }

        public async Task<(bool, string)> AddPartnerAsync(Partner partner)
        {
            try
            {
                // get webhook token.
                await _database.AddAsync(partner);
                await _database.SaveChangesAsync();

                if (partner.Active)
                    _channelVerification.AddPartner(partner);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _rest.Logger.LogWarning($"Failed to add {partner.GuildId} to the database: {ex.Message}");
                return (false, "Partner failed to be added.");
            }
        }

        /// <summary>
        /// Removes and delets a partner from the database. Use UpdatePartnerAsync to disable partners.
        /// </summary>
        /// <param name="guildId">Guild ID of the partner to remove.</param>
        /// <returns>The removed partner</returns>
        public async Task<(Partner?, string)> RemovePartnerAsync(ulong guildId)
        {
            var partner = await _database.FindAsync<Partner>(guildId);

            if (partner is null) return (null, "No partner found for that ID");

            _ = _database.Remove(partner);

            await _database.SaveChangesAsync();

            _channelVerification.RemovePartner(partner);

            return (partner, string.Empty);
        }

        public async Task<(bool, string)> UpdateWebhookValueAsync(ulong guildId, ulong newChannelId)
        {
            var partner = await _database.FindAsync<Partner>(guildId);

            if (partner is null) return (false, "No partner found for that ID");

            var hook = await _rest.GetWebhookAsync(partner.WebhookId);
            await hook.ModifyAsync(channelId: newChannelId, reason: "Partner Bot Sender Webhook Update");

            return (true, string.Empty);
        }

        public async Task<(Partner?, string)> UpdatePartnerAsync(ulong guildId, Func<PartnerUpdater> update)
        {
            var data = update.Invoke();

            var p = await _database.FindAsync<Partner>(guildId);

            if (p is null) return (null, "No partner found for that ID");

            if (data.OwnerId is not null)
                p.OwnerId = data.OwnerId.Value;

            if (data.Message is not null)
                p.Message = data.Message;

            if (data.DonorRank is not null)
                p.DonorRank = data.DonorRank.Value;

            if (data.Banner is not null)
                p.Banner = data.Banner;

            if (data.Tags is not null)
                p.SetTags(data.Tags);

            if (data.Invite is not null)
                p.Invite = data.Invite;

            if (data.NSFW is not null)
                p.NSFW = data.NSFW.Value;

            if (data.ReceiveNSFW is not null)
                p.ReceiveNSFW = data.ReceiveNSFW.Value;

            await _database.SaveChangesAsync();

            if (data.Active is not null)
            {
                if (data.Active.Value)
                {
                    var res = await EnablePartnerAsync(p.GuildId);
                    if (!res.Item1)
                        return (null, res.Item2);
                }
                else
                {
                    var res = await DisablePartnerAsync(p.GuildId);
                    if (!res.Item1)
                        return (null, res.Item2);
                }
            }

            if(data.ChannelId is not null)
            {
                await UpdateWebhookValueAsync(guildId, data.ChannelId.Value);
            }

            return (p, string.Empty);
        }

        internal async Task<(bool, string)> DisablePartnerAsync(ulong guildId)
        {
            var partner = await _database.FindAsync<Partner>(guildId);

            if (partner is null) return (false, "No partner by that ID found");

            partner.Active = false;
            await _database.SaveChangesAsync();

            // Run internal setups to deactivate a partner.
            _channelVerification.RemovePartner(partner);

            return (true, string.Empty);
        }

        internal async Task<(bool, string)> EnablePartnerAsync(ulong guildId)
        {
            var partner = await _database.FindAsync<Partner>(guildId);

            if (partner is null) return (false, "No partner by that ID found");

            partner.Active = true;
            await _database.SaveChangesAsync();

            // Run internal setps to activate a partner.
            _channelVerification.AddPartner(partner);

            return (true, string.Empty);
        }
    }
}
