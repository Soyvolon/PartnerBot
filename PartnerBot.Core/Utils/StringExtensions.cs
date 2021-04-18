using System;
using System.Collections.Generic;

namespace PartnerBot.Core.Utils
{
    public static class StringExtensions
    {
        /// <summary>
        /// Gets the words in a string that contain URLs. If a url is attached to a word, ie (this)[url], the entire block is returned.
        /// </summary>
        /// <param name="content">String the serach through.</param>
        /// <returns>List or URL blocks in the order they are found.</returns>
        public static IReadOnlyList<string> GetUrls(this string content)
        {
            List<string> data = new();

            if (string.IsNullOrWhiteSpace(content)) return data;

            string[]? body = content.Split(" \n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            foreach(string? part in body)
            {
                if (part.ContainsUrl() || part.ContainsDiscordUrl())
                    data.Add(part);
            }

            return data;
        }

        public static bool ContainsUrl(this string s)
            => s.Contains("http://", StringComparison.OrdinalIgnoreCase)
                || s.Contains("https://", StringComparison.OrdinalIgnoreCase)
                || s.Contains("www.");

        public static bool ContainsDiscordUrl(this string s)
            => s.Contains("discord.gg");
    }
}
