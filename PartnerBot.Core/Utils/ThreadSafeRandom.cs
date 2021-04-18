using System;

namespace PartnerBot.Core.Utils
{
    public class ThreadSafeRandom
    {
        private static readonly Random _global = new Random();
        [ThreadStatic]
        private static Random _local;

        public static int Next(int minValue, int maxValue)
        {
            Random inst = _local;
            if(inst is null)
            {
                int seed;
                lock (_global) seed = _global.Next();
                _local = inst = new Random(seed);
            }

            return inst.Next(minValue, maxValue);
        }

        public static int Next(int maxValue)
        {
            return Next(0, maxValue);
        }

        public static int Next()
        {
            return Next(0, int.MaxValue);
        }
    }
}
