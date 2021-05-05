using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;
using PartnerBot.Core.Utils;

using Soyvolon.Utilities.Extensions.IList;

namespace PartnerBot.Core.Services
{
    /// <summary>
    /// The service responsible for sending Partners
    /// </summary>
    public class PartnerSenderService
    {
        private readonly IServiceProvider _services;
        private readonly DiscordRestClient _rest;
        private readonly ILogger _logger;
        private readonly PartnerManagerService _partners;
        private ConcurrentQueue<PartnerData> PartnerDataQueue { get; init; }
        private ConcurrentDictionary<ulong, ConcurrentQueue<ulong>> Cache { get; init; }
        private const int CAHCE_MAX_SIZE = 24;

        public PartnerSenderService(IServiceProvider services, DiscordRestClient rest, PartnerManagerService partners)
        {
            this._services = services;
            this._rest = rest;
            this._logger = this._rest.Logger;
            this._partners = partners;

            this.PartnerDataQueue = new();
            this.Cache = new();
        }

        public async Task ExecuteAsync(PartnerSenderArguments senderArgs)
        {
            this._logger.LogInformation($"Started partner run: {senderArgs}");

            (List<Partner>, List<Partner>, List<Partner>) active;
            if (senderArgs.DevelopmentStressTest)
            {
                // Get the development stress test data set ...
                active = await GetDevelopmentStressTestDataset(senderArgs);
            }
            else
            {
                // Get all active partners ...
                active = await GetActivePartners(senderArgs);
            }

            this._logger.LogInformation($"Partner sets received. Full: {active.Item1.Count} | Run: {active.Item2.Count} | Extra: {active.Item3.Count}");

            if (active.Item1.Count <= 0 || active.Item2.Count <= 0)
                return;

            // ... Pair up active partners and queue them ...
            await QueuePartnerData(active.Item1, active.Item2, active.Item3, senderArgs);

            this._logger.LogInformation($"Partner data queued. Queue Count: {this.PartnerDataQueue.Count}");

            // ... Then execute the queue (off with its head!).
            await ExecuteWebhooksAsync(senderArgs);

            this._logger.LogInformation($"Webhooks are finished.");
        }

        private async Task ExecuteWebhooksAsync(PartnerSenderArguments senderArguments)
        {
            this._logger.LogInformation("Starting webhook execution.");

            while(this.PartnerDataQueue.TryDequeue(out PartnerData? data))
            {
                try
                {
                    await data.ExecuteAsync(this._rest, senderArguments);
                }
                catch (Exception ex)
                {
                    this._logger.LogWarning(ex, $"Failed to execute Partner Webhook for guild {data.GuildId}, disabiling partner.");

                    _ = Task.Run(async () =>
                    {
                        if (ex is BadRequestException)
                        {
                            await this._rest.ExecuteWebhookAsync(data.Match.WebhookId, data.Match.WebhookToken, new DiscordWebhookBuilder()
                                .AddEmbed(new DiscordEmbedBuilder()
                                    .WithColor(DiscordColor.DarkRed)
                                    .WithDescription("Partner Bot has been disabled on your server because your Partner Message has exceeded 1900 characters," +
                                    " or your setup is otherwise invalid. Please use `pb!setup` and reconfigure Partner Bot.")));

                            await _partners.UpdateOrAddPartnerAsync(data.Match.GuildId, () => new()
                            {
                                Active = false
                            });
                        }
                        else
                        {
                            _logger.LogWarning(ex, "Partner Sender Failed");
                        }
                    });

                    continue;
                }
            }
        }

        private async Task<(List<Partner>, List<Partner>, List<Partner>)> GetActivePartners(PartnerSenderArguments senderArgs)
        {
            List<Partner> baseSet = new();
            List<Partner> donorSet = new();
            List<Partner> extarSet = new();

            using var scope = this._services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<PartnerDatabaseContext>();
            await database.Partners.AsNoTracking().ForEachAsync(x =>
            {
                if (x.Active)
                {
                    baseSet.Add(x);

                    if (x.DonorRank >= senderArgs.DonorRun)
                        donorSet.Add(x);

                    if (x.DonorRank >= 2)
                        extarSet.Add(x);
                }
            });

            return (baseSet, donorSet, extarSet);
        }

        #region Partner Matching
        private async Task QueuePartnerData(List<Partner> runningPartners, List<Partner> fullSet, List<Partner> extraSet, PartnerSenderArguments senderArguments)
        {
            // Build the task storage for our closeness tasks ...
            List<Task<(ulong, List<(float, Partner)>)>> MatchClosenessTasks = new();
            // ... then get a closeness list for each partner ...
            foreach (Partner? p in runningPartners)
            { // ... by letting the get match closeness method run for each partner ...
                MatchClosenessTasks.Add(GetMatchCloseness(p, runningPartners, senderArguments));
            }
            // ... while that runs, lets scramble and bag all of the partners ...

            // ... lets get our list ...
            List<Partner>? pList = runningPartners;
            // ... then randomize it ...
            pList.Shuffle();
            // ... so we create the match bag with it ...
            ConcurrentBag<Partner> MatchBag = new(pList);

            // ... once we have the bag, we create the storage for the closenss results ...
            ConcurrentDictionary<ulong, List<(float, Partner)>> ClosenessResults = new();
            // ... then wait for the closeness tasks to finish ...
            foreach (Task<(ulong, List<(float, Partner)>)>? t in MatchClosenessTasks)
            {
                (ulong, List<(float, Partner)>) res = await t;
                ClosenessResults[res.Item1] = res.Item2;
            }
            // ... then create a hashset to store the matched guilds ...
            HashSet<ulong> MatchedSet = new();
            // ... then for each item in the bag ...

            List<Partner>? fullList = null;

            while(MatchBag.TryTake(out Partner? p))
            {
                // ... skip this instance if this guild has already been matched ...
                if (MatchedSet.Contains(p.GuildId)) continue;

                // ... otherwise, get the match results ...
                List<(float, Partner)>? res = ClosenessResults[p.GuildId];
                // ... for all the items in the results ...
                bool matched = false;
                foreach((float, Partner) i in res)
                {
                    // ... skip the item if it has been matched ...
                    if (MatchedSet.Contains(i.Item2.GuildId)) continue;

                    // ... if none of those are true, we have a match, so lets save it ...
                    PartnerData? homeMatch = p.BuildData(i.Item2, null);
                    PartnerData? awayMatch = i.Item2.BuildData(p, null);

                    // ... then we queue these matches ...
                    this.PartnerDataQueue.Enqueue(homeMatch);
                    this.PartnerDataQueue.Enqueue(awayMatch);

                    // ... finally store the matches ...
                    MatchedSet.Add(p.GuildId);
                    MatchedSet.Add(i.Item2.GuildId);
                    // ... if cacheing is not turned off, cache this match ...
                    if (!senderArguments.IgnoreCacheMatch)
                        CacheMatch(p.GuildId, i.Item2.GuildId);

                    matched = true;
                    break;
                }
                // ... if the value was matched, go to the next value ...
                if (matched) continue;
                // ... otherwise, make sure we have a full list to pick values from ...
                if (fullList is null)
                    fullList = fullSet;

                if (fullSet.Count <= 1) return;

                // ... then get a random value that does not fail any paring options (no same owner, not cached) ...
                Partner? away = PickPartner(p, fullList, senderArguments);
                // Looks like no good match was found, go to next partner.
                if (away is null) continue;

                PartnerData? homeMatchAlt = p.BuildData(away, null);

                Partner? extra = null;

                if (away.DonorRank < senderArguments.DonorRun)
                {
                    int breakout = 0;

                    while(extra is null && breakout++ < 10)
                    {
                        Partner? temp = extraSet.GetRandom();

                        if (away.OwnerId != temp.OwnerId)
                        {
                            extra = temp;
                        }    
                    }
                }

                if (MatchedSet.Contains(away.GuildId))
                    extra = null;

                PartnerData? awayMatchAlt = away.BuildData(p, extra);

                // ... then we queue these matches ...
                this.PartnerDataQueue.Enqueue(homeMatchAlt);
                this.PartnerDataQueue.Enqueue(awayMatchAlt);

                // ... finally store the matches ...
                MatchedSet.Add(p.GuildId);
                MatchedSet.Add(away.GuildId);
                // ... if cacheing is not turned off, cache this match.
                if (!senderArguments.IgnoreCacheMatch)
                    CacheMatch(p.GuildId, away.GuildId);
            }
        }

        private Partner? PickPartner(Partner home, List<Partner> fullList, PartnerSenderArguments senderArguments)
        {
            Partner away;
            int fail = 0;
            do
            {
                away = fullList[ThreadSafeRandom.Next(0, fullList.Count)];
            } while ((away.OwnerId == home.OwnerId ||
                (!senderArguments.IgnoreCacheMatch && CheckCache(home.GuildId, away.GuildId)))
                && fail++ < 10);

            if (fail >= 10)
                return null;

            return away;
        }

        #endregion

        #region Cache
        private void CacheMatch(ulong homeId, ulong awayId)
        {
            // Cache holds data for a single day, so no cache set should be over 24 items (CACHE_MAX_SIZE).
            if (!this.Cache.ContainsKey(homeId))
                this.Cache[homeId] = new();

            this.Cache[homeId].Enqueue(awayId);

            while (this.Cache[homeId].Count > CAHCE_MAX_SIZE)
                _ = this.Cache[homeId].TryDequeue(out _);

            // Same thing, but for the away id.
            if (!this.Cache.ContainsKey(awayId))
                this.Cache[awayId] = new();

            this.Cache[awayId].Enqueue(homeId);

            while (this.Cache[awayId].Count > CAHCE_MAX_SIZE)
                _ = this.Cache[awayId].TryDequeue(out _);
        }

        private bool CheckCache(ulong homeId, ulong awayId)
        {
            // Make sure this guild hasnt been matched with recently.
            if (this.Cache.TryGetValue(homeId, out ConcurrentQueue<ulong>? homeCache))
            {
                if (homeCache.Contains(awayId)) return true;
            }

            if(this.Cache.TryGetValue(awayId, out ConcurrentQueue<ulong>? awayCache))
            {
                if (awayCache.Contains(homeId)) return true;
            }

            return false;
        }

        #endregion

        #region Match Closeness

        private Task<(ulong, List<(float, Partner)>)> GetMatchCloseness(Partner toMatch, List<Partner> fullList, PartnerSenderArguments senderArguments)
        {
            // We want to get a value between 0 and 1, where 1 is a perfect match and 0 is a very bad match.
            // The match should compare tags, the server owner, the cahced servers, and server size.

            List<(float, Partner)> matches = new();
            // For every partner in the full list ...
            foreach (Partner? item in fullList)
            {
                float match = 1.0f;
                // ... ignore values that are by the same owner ...
                if (!senderArguments.IgnoreOwnerMatch)
                    if (toMatch.OwnerId == item.OwnerId) continue;
                // ... and those saved in the cache ...
                if (!senderArguments.IgnoreCacheMatch)
                    if (CheckCache(toMatch.GuildId, item.GuildId)) continue;

                // ... and check the compatability of their NSFW states ...
                if (!senderArguments.IgnoreNSFWMatch)
                {
                    if (toMatch.NSFW)
                    {
                        if (!(item.ReceiveNSFW || item.NSFW)) continue;
                    }
                    else
                    {
                        if (item.NSFW && !toMatch.ReceiveNSFW) continue;
                    }
                }

                float matchedTags = 0.0f;
                // ... for every tag in the to match ...
                HashSet<string>? itemTags = item.Tags;
                HashSet<string>? matchTags = toMatch.Tags;
                foreach(string? t in matchTags)
                { // ... add one if there is a matching tag in the potential parter ...
                    if (itemTags.Contains(t))
                        matchedTags++;
                }
                // ... then get the larger of the two tag counts ...
                int largestTagCount = Math.Max(matchTags.Count, itemTags.Count);
                // ... and the percentage of tags that match each other ...
                float tagMatchPercentage = matchedTags / largestTagCount;
                // ... then multiply the starting value by the percentage ...
                match *= tagMatchPercentage;
                // ... then if both partners have a registered user count ...
                if(toMatch.UserCount != -1 && item.UserCount != -1)
                { // ... divide the difference in user counts by the larger of the two partners ...
                    float dif = (float)Math.Abs(toMatch.UserCount - item.UserCount) / Math.Max(toMatch.UserCount, item.UserCount);
                    // ... and multiply that value by the match value ...
                    match *= dif;
                }
                // ... then add it as a value of the matches array.
                matches.Add((match, item));
            }

            matches.Sort((x, y) => y.Item1.CompareTo(x.Item1));

            return Task.FromResult((toMatch.GuildId, matches));
        }

        #endregion

        #region Development Dataset
        private async Task<(List<Partner>, List<Partner>, List<Partner>)> GetDevelopmentStressTestDataset(PartnerSenderArguments senderArguments)
        {
            List<DevelopmentStressTestChannel>? data = await DevelopmentStressTestDataManager.GetDataAsync();

            if (data is null)
                throw new Exception("No test set data was found.");

            List<Partner> fullSet = new();

            ulong lastOwner = 0;
            foreach(DevelopmentStressTestChannel? item in data)
            {
                Partner p = new()
                {
                    Active = true,
                    DonorRank = ThreadSafeRandom.Next(0, 4),
                    GuildId = item.ChannelId,
                    OwnerId = item.ChannelId,
                    WebhookId = item.WebhookId,
                    WebhookToken = item.WebhookToken,
                    NSFW = ThreadSafeRandom.Next(1, 20) < 2,
                    UserCount = ThreadSafeRandom.Next(1, 10001)
                };

                p.Tags.UnionWith(item.Tags);

                if (lastOwner != 0)
                {
                    int num = ThreadSafeRandom.Next(0, 100);
                    if (num < 5)
                        p.OwnerId = lastOwner;
                }

                p.ReceiveNSFW = p.NSFW ? true : ThreadSafeRandom.Next(1, 20) < 10;

                fullSet.Add(p);

                lastOwner = item.ChannelId;
            }

            List<Partner> donorSet = new(fullSet.Where(x => x.DonorRank >= senderArguments.DonorRun));

            return (donorSet, fullSet, fullSet);
        }
        #endregion
    }
}
