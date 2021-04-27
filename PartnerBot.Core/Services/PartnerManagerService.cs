using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;

namespace PartnerBot.Core.Services
{
    /// <summary>
    /// The service responsible for updating Partners
    /// </summary>
    public class PartnerManagerService
    {
        private readonly GuildVerificationService _channelVerification;
        private readonly DiscordRestClient _rest;
        private readonly IServiceProvider _services;

        public PartnerManagerService(GuildVerificationService channelVerification,
            DiscordRestClient rest, IServiceProvider services)
        {
            this._channelVerification = channelVerification;
            this._rest = rest;
            _services = services;
        }

        public async Task<(bool, string)> AddPartnerAsync(Partner partner)
        {
            try
            {
                using var scope = this._services.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<PartnerDatabaseContext>();
                // get webhook token.
                await database.AddAsync(partner);
                await database.SaveChangesAsync();

                if (partner.Active)
                    this._channelVerification.AddPartner(partner);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                this._rest.Logger.LogWarning($"Failed to add {partner.GuildId} to the database: {ex.Message}");
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
            using var scope = this._services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<PartnerDatabaseContext>();
            Partner? partner = await database.FindAsync<Partner>(guildId);

            if (partner is null) return (null, "No partner found for that ID");

            _ = database.Remove(partner);

            await database.SaveChangesAsync();

            this._channelVerification.RemovePartner(partner);

            return (partner, string.Empty);
        }

        public async Task<(Partner?, string)> UpdateOrAddPartnerAsync(ulong guildId, Func<PartnerUpdater> update)
        {
            PartnerUpdater? data = update.Invoke();
            using var scope = this._services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<PartnerDatabaseContext>();
            Partner? p = await database.FindAsync<Partner>(guildId);

            if (p is null)
            {
                p = new()
                {
                    GuildId = guildId
                };
                await database.AddAsync(p);
                await database.SaveChangesAsync();
            }

            if (data.GuildIcon is not null)
                p.GuildIcon = data.GuildIcon;

            if (data.GuildName is not null)
                p.GuildName = data.GuildName;

            if (data.OwnerId is not null)
                p.OwnerId = data.OwnerId.Value;

            if (data.Message is not null)
                p.Message = data.Message;

            bool donorCheck = false;
            if (data.DonorRank is not null)
            {
                if (p.DonorRank != data.DonorRank.Value)
                {
                    p.DonorRank = data.DonorRank.Value;
                    donorCheck = true;
                }
            }

            if (data.Banner is not null)
                p.Banner = data.Banner;

            if (data.TagOverride is null)
            {
                if (data.TagsToAdd is not null)
                    p.Tags.UnionWith(data.TagsToAdd);

                if (data.TagsToRemove is not null)
                    p.Tags.ExceptWith(data.TagsToRemove);
            }
            else
            {
                p.Tags = data.TagOverride;
            }

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

            if (data.VanityInvite.Item1)
                p.VanityInvite = data.VanityInvite.Item2;

            if (data.Active is not null)
            {
                if (data.Active.Value != p.Active)
                {
                    if (data.Active.Value)
                    {
                        this._channelVerification.AddPartner(p);
                        p.Active = true;
                    }
                    else
                    {
                        this._channelVerification.RemovePartner(p);
                        p.Active = false;
                    }
                }
            }

            if (data.ChannelId is not null)
            {
                if (data.ChannelId == 0)
                {
                    p.WebhookToken = "";
                    p.WebhookId = 0;
                }
                else
                {
                    bool inviteUpdate = p.VanityInvite is null;
                    DiscordWebhook hook;
                    DiscordInvite? invite = null;
                    if (p.WebhookId == 0)
                    {
                        hook = await this._rest.CreateWebhookAsync(data.ChannelId.Value, "Partner Bot Message Sender", reason: "Partner Bot Sender Webhook Update");
                        if(inviteUpdate)
                            invite = await this._rest.CreateChannelInviteAsync(data.ChannelId.Value, 0, 0, false, false, "Partner Bot Inivte");
                    }
                    else
                    {
                        bool updateWebhook = false;
                        try
                        {
                            hook = await this._rest.GetWebhookAsync(p.WebhookId);
                            if (inviteUpdate)
                                invite = await this._rest.CreateChannelInviteAsync(data.ChannelId.Value, 0, 0, false, false, "Partner Bot Inivte");

                            updateWebhook = hook.ChannelId != data.ChannelId;
                        }
                        catch (NotFoundException)
                        {
                            hook = await this._rest.CreateWebhookAsync(data.ChannelId.Value, "Partner Bot Message Sender", reason: "Partner Bot Sender Webhook Update");
                            if (inviteUpdate)
                                invite = await this._rest.CreateChannelInviteAsync(data.ChannelId.Value, 0, 0, false, false, "Partner Bot Inivte");
                        }
                        catch (Exception ex)
                        {
                            return (null, ex.Message);
                        }

                        if (updateWebhook)
                            await hook.ModifyAsync("Partner Bot Message Sender", channelId: data.ChannelId.Value, reason: "Partner Bot Sender Webhook Update");
                    }

                    p.WebhookId = hook.Id;
                    p.WebhookToken = hook.Token;
                    if(invite is not null)
                        p.Invite = invite.Code;
                }
            }

            if (donorCheck)
                p.ModifyToDonorRank();

            database.Update(p);
            await database.SaveChangesAsync();

            return (p, string.Empty);
        }

        public async Task<TProperty?> GetPartnerElementAsync<TProperty>(ulong guildId, Expression<Func<Partner, TProperty>> propertyExpression)
        {
            using var scope = this._services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<PartnerDatabaseContext>();
            Partner? p = await database.FindAsync<Partner>(guildId);

            if (p is null) return default;

            Func<Partner, TProperty>? exp = propertyExpression.Compile();

            return exp.Invoke(p);
        }
    }
}
