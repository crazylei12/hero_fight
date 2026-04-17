using Fight.Battle;
using Fight.Data;

namespace Fight.Core
{
    public static class DamageResolver
    {
        public static float ResolveDamage(float attackPower, float criticalChance, float criticalDamageMultiplier, float defense, BattleRandomService randomService, float powerMultiplier)
        {
            if (attackPower <= 0f || powerMultiplier <= 0f)
            {
                return 0f;
            }

            var baseDamage = attackPower * powerMultiplier;
            if (baseDamage <= 0f)
            {
                return 0f;
            }

            var isCritical = CriticalResolver.RollCritical(criticalChance, randomService);
            var critMultiplier = isCritical ? criticalDamageMultiplier : 1f;
            var mitigatedDamage = baseDamage * critMultiplier * CalculateDefenseMultiplier(defense);
            return mitigatedDamage;
        }

        private static float CalculateDefenseMultiplier(float defense)
        {
            return 100f / (100f + defense);
        }
    }
}
