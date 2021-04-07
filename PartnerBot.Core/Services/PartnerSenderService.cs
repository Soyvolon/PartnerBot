using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;

using Microsoft.EntityFrameworkCore;

using PartnerBot.Core.Database;
using PartnerBot.Core.Entities;
using PartnerBot.Core.Utils;

namespace PartnerBot.Core.Services
{
    public class PartnerSenderService
    {
        private readonly PartnerDatabaseContext _database;
        private readonly DiscordRestClient _rest;
        private ConcurrentQueue<PartnerData> PartnerDataQueue { get; init; }
        private ConcurrentDictionary<ulong, ConcurrentQueue<ulong>> Cache { get; init; }
        private const int CAHCE_MAX_SIZE = 24;

        public PartnerSenderService(PartnerDatabaseContext database, DiscordRestClient rest)
        {
            _database = database;
            _rest = rest;

            PartnerDataQueue = new();
            Cache = new();
        }

        public async Task ExecuteAsync(PartnerSenderArguments senderArgs)
        {
            (List<Partner>, List<Partner>) active;
            if (senderArgs.DevelopmentStressTest)
            {
                // get the development stress test data set ...
                active = await GetDevelopmentStressTestDataset(senderArgs);
            }
            else
            {
                // get all active partners ...
                active = await GetActivePartners(senderArgs);
            }

            // ... pair up active partners and queue them ...
            await QueuePartnerData(active.Item1, active.Item2, senderArgs);

            // ... then execute the queue (off with its head!)
            await ExecuteWebhooksAsync(senderArgs);
        }

        private async Task ExecuteWebhooksAsync(PartnerSenderArguments senderArguments)
        {
            while(PartnerDataQueue.TryDequeue(out var data))
            {
                await data.ExecuteAsync(_rest, senderArguments);
            }
        }

        private async Task<(List<Partner>, List<Partner>)> GetActivePartners(PartnerSenderArguments senderArgs)
        {
            List<Partner> baseSet = new();
            List<Partner> donorSet = new();
            await _database.Partners.AsNoTracking().ForEachAsync(x =>
            {
                if (x.Active)
                {
                    baseSet.Add(x);

                    if (x.DonorRank >= senderArgs.DonorRun)
                        donorSet.Add(x);
                }
            });

            return (baseSet, donorSet);
        }

        #region Partner Matching

        // TODO: check for pairs that don't find a match, filter then into the fullset and grabe a random partner without knowing its final value.
        private async Task QueuePartnerData(List<Partner> runningPartners, List<Partner> fullSet, PartnerSenderArguments senderArguments)
        {
            // ... then build the task storage for our closeness tasks ...
            List<Task<(ulong, List<(float, Partner)>)>> MatchClosenessTasks = new();
            // ... then get a closeness list for each partner ...
            foreach (var p in runningPartners)
            { // ... by letting the get match closeness method run for each partner ...
                MatchClosenessTasks.Add(GetMatchCloseness(p, runningPartners, senderArguments));
            }
            // ... while that runs, lets scramble and bag all of the partners ...

            // ... lets get our list ...
            var pList = runningPartners;
            // ... then randomize it ...
            pList.Shuffle();
            // ... so we create the match bag with it ...
            ConcurrentBag<Partner> MatchBag = new(pList);

            // ... once we have the bag, we create the storage for the closenss results ...
            ConcurrentDictionary<ulong, List<(float, Partner)>> ClosenessResults = new();
            // ... then wait for the closeness tasks to finish ...
            foreach (var t in MatchClosenessTasks)
            {
                var res = await t;
                ClosenessResults[res.Item1] = res.Item2;
            }
            // ... then create a hashset to store the matched guilds ...
            HashSet<ulong> MatchedSet = new();
            // ... then for each item in the bag ...

            List<Partner>? fullList = null;

            while(MatchBag.TryTake(out var p))
            {
                // ... skip this instance if this guild has already been matched ...
                if (MatchedSet.Contains(p.GuildId)) continue;

                // ... otherwise, get the match results ...
                var res = ClosenessResults[p.GuildId];
                // ... for all the items in the results ...
                bool matched = false;
                foreach(var i in res)
                {
                    // ... skip the item if it has been matched ...
                    if (MatchedSet.Contains(i.Item2.GuildId)) continue;

                    // ... if none of those are true, we have a match, so lets save it ...
                    var homeMatch = p.BuildData(i.Item2, false);
                    var awayMatch = i.Item2.BuildData(p, false);

                    // ... then we queue these matches ...
                    PartnerDataQueue.Enqueue(homeMatch);
                    PartnerDataQueue.Enqueue(awayMatch);

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

                // ... then get a random value that does not fail any paring options (no same owner, not cached) ...
                var away = PickPartner(p, fullList, senderArguments);

                var homeMatchAlt = p.BuildData(away, false);

                var extra = false;

                if (away.DonorRank < senderArguments.DonorRun)
                    extra = true;

                if (MatchedSet.Contains(away.GuildId))
                    extra = true;

                var awayMatchAlt = away.BuildData(p, extra);

                // ... then we queue these matches ...
                PartnerDataQueue.Enqueue(homeMatchAlt);
                PartnerDataQueue.Enqueue(awayMatchAlt);

                // ... finally store the matches ...
                MatchedSet.Add(p.GuildId);
                MatchedSet.Add(away.GuildId);
                // ... if cacheing is not turned off, cache this match ...
                if (!senderArguments.IgnoreCacheMatch)
                    CacheMatch(p.GuildId, away.GuildId);
            }
        }

        private Partner PickPartner(Partner home, List<Partner> fullList, PartnerSenderArguments senderArguments)
        {
            Partner away;
            do
            {
                away = fullList[ThreadSafeRandom.Next(0, fullList.Count)];
            } while (away.OwnerId == home.OwnerId ||
                (!senderArguments.IgnoreCacheMatch && CheckCache(home.GuildId, away.GuildId)));

            return away;
        }

        #endregion

        #region Cache
        private void CacheMatch(ulong homeId, ulong awayId)
        {
            // Cache holds data for a single day, so no cache set should be over 24 items (CACHE_MAX_SIZE).
            if (!Cache.ContainsKey(homeId))
                Cache[homeId] = new();

            Cache[homeId].Enqueue(awayId);

            while (Cache[homeId].Count > CAHCE_MAX_SIZE)
                _ = Cache[homeId].TryDequeue(out _);

            // same thig, but for the away id
            if (!Cache.ContainsKey(awayId))
                Cache[awayId] = new();

            Cache[awayId].Enqueue(homeId);

            while (Cache[awayId].Count > CAHCE_MAX_SIZE)
                _ = Cache[awayId].TryDequeue(out _);
        }

        private bool CheckCache(ulong homeId, ulong awayId)
        {
            // ... make sure this guild hasnt been matched with recently ...
            if (Cache.TryGetValue(homeId, out var homeCache))
            {
                if (homeCache.Contains(awayId)) return true;
            }

            if(Cache.TryGetValue(awayId, out var awayCache))
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
            // ... for every partner in the full list ...
            foreach(var item in fullList)
            {
                float match = 1.0f;
                // ... ignore values that are by the same owner ...
                if (!senderArguments.IgnoreOwnerMatch)
                    if (toMatch.OwnerId == item.OwnerId) continue;
                // ... and those saved in the cache ...
                if (!senderArguments.IgnoreCacheMatch)
                    if (CheckCache(toMatch.GuildId, item.GuildId)) continue;

                float matchedTags = 0.0f;
                // ... for every tag in the to match ...
                var itemTags = item.Tags;
                var matchTags = toMatch.Tags;
                foreach(var t in matchTags)
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
        private async Task<(List<Partner>, List<Partner>)> GetDevelopmentStressTestDataset(PartnerSenderArguments senderArguments)
        {
            var data = await DevelopmentStressTestDataManager.GetDataAsync();

            if (data is null)
                throw new Exception("No test set data was found.");

            List<Partner> fullSet = new();

            ulong lastOwner = 0;
            foreach(var item in data)
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
                    var num = ThreadSafeRandom.Next(0, 100);
                    if (num < 5)
                        p.OwnerId = lastOwner;
                }

                p.ReceiveNSFW = p.NSFW ? true : ThreadSafeRandom.Next(1, 20) < 10;

                fullSet.Add(p);

                lastOwner = item.ChannelId;
            }

            List<Partner> donorSet = new(fullSet.Where(x => x.DonorRank >= senderArguments.DonorRun));

            return (donorSet, fullSet);
        }
        #endregion
    }
}
