using System;

namespace Fight.Battle
{
    public class BattleRandomService
    {
        private readonly System.Random random;

        public BattleRandomService(int? seed = null)
        {
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public float NextFloat()
        {
            return (float)random.NextDouble();
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            if (maxInclusive <= minInclusive)
            {
                return minInclusive;
            }

            return minInclusive + ((float)random.NextDouble() * (maxInclusive - minInclusive));
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }
    }
}
