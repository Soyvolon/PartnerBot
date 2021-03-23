using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

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

        public async Task<(bool, string)> UpdateOrAddPartnerAsync(ulong guildId, Func<PartnerUpdater> update)
        {
            var data = update.Invoke();

            var p = await _database.FindAsync<Partner>(guildId);

            if (p is null)
            {
                p = new();
                await _database.AddAsync(p);
                await _database.SaveChangesAsync();
            }

            if (data.OwnerId is not null)
                p.OwnerId = data.OwnerId.Value;

            if (data.Message is not null)
                p.Message = data.Message;

            if (data.DonorRank is not null)
                p.DonorRank = data.DonorRank.Value;

            if (data.Banner is not null)
                p.Banner = data.Banner;

            if (data.TagsToAdd is not null)
                p.Tags.UnionWith(data.TagsToAdd);

            if (data.TagsToRemove is not null)
                p.Tags.ExceptWith(data.TagsToRemove);

            if (data.Invite is not null)
                p.Invite = data.Invite;

            if (data.NSFW is not null)
                p.NSFW = data.NSFW.Value;

            if (data.ReceiveNSFW is not null)
                p.ReceiveNSFW = data.ReceiveNSFW.Value;

            if (data.UserCount is not null)
                p.UserCount = data.UserCount.Value;

            if (data.LinksUsed is not null)
                p.LinksUsed = data.LinksUsed.Value;

            if (data.BaseColor is not null)
                p.BaseColor = data.BaseColor.Value;

            if (data.MessageEmbeds is not null)
                p.MessageEmbeds = data.MessageEmbeds;

            if (data.Active is not null)
            {
                if (data.Active.Value != p.Active)
                {
                    if (data.Active.Value)
                    {
                        _channelVerification.AddPartner(p);
                        p.Active = true;
                    }
                    else
                    {
                        _channelVerification.RemovePartner(p);
                        p.Active = false;
                    }
                }
            }

            if (data.ChannelId is not null)
            {
                if (p.WebhookId != data.ChannelId)
                {
                    DiscordWebhook? hook = null;
                    if (p.WebhookId == 0)
                    {
                        hook = await _rest.CreateWebhookAsync(data.ChannelId.Value, "Partner Bot Message Sender", reason: "Partner Bot Sender Webhook Update");
                    }

                    try
                    {
                        if (hook is null)
                            hook = await _rest.GetWebhookAsync(p.WebhookId);
                    }
                    catch (NotFoundException)
                    {
                        hook = await _rest.CreateWebhookAsync(data.ChannelId.Value, "Partner Bot Message Sender", reason: "Partner Bot Sender Webhook Update");
                    }
                    catch (Exception ex)
                    {
                        return (false, ex.Message);
                    }

                    await hook.ModifyAsync(channelId: data.ChannelId.Value, reason: "Partner Bot Sender Webhook Update");

                    p.WebhookId = hook.Id;
                    p.WebhookToken = hook.Token;
                }
            }

            _database.Update(p);
            await _database.SaveChangesAsync();

            return (true, string.Empty);
        }

        public static async Task<(ulong, string)?> RegisterNewWebhook(ulong channelId)
        {
            throw new NotImplementedException();
        }
    }
}
