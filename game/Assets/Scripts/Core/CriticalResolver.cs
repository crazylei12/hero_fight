using Fight.Battle;
using Fight.Data;

namespace Fight.Core
{
    public static class CriticalResolver
    {
        public static bool RollCritical(HeroDefinition attackerDefinition, BattleRandomService randomService)
        {
            if (attackerDefinition == null || randomService == null)
            {
                return false;
            }

            return RollCritical(attackerDefinition.baseStats.criticalChance, randomService);
        }

        public static bool RollCritical(float criticalChance, BattleRandomService randomService)
        {
            if (randomService == null || criticalChance <= 0f)
            {
                return false;
            }

            return randomService.NextFloat() < criticalChance;
        }
    }
}
