using System.Collections.Generic;

namespace PartnerBot.Core.Utils
{
    public static class IListExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int count = list.Count;
            int last = count - 1;
            for (int i = 0; i < last; ++i)
            {
                int r = ThreadSafeRandom.Next(i, count);
                T? tmp = list[i];
                list[i] = list[r];
                list[r] = tmp;
            }
        }
    }
}
