using System.Collections.Generic;

namespace PartnerBot.Core.Utils
{
    public static class IListExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            var count = list.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = ThreadSafeRandom.Next(i, count);
                var tmp = list[i];
                list[i] = list[r];
                list[r] = tmp;
            }
        }
    }
}
