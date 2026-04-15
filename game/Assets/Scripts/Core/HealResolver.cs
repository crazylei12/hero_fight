using Fight.Data;
using Fight.Heroes;

namespace Fight.Core
{
    public static class HealResolver
    {
        public static float ResolveHealAmount(RuntimeHero caster, SkillData skillData)
        {
            if (caster == null || skillData == null)
            {
                return 0f;
            }

            if (skillData.effects != null)
            {
                for (var i = 0; i < skillData.effects.Count; i++)
                {
                    var effect = skillData.effects[i];
                    if (effect.effectType == SkillEffectType.DirectHeal)
                    {
                        return ResolveHealAmount(caster, effect.powerMultiplier);
                    }
                }
            }

            return ResolveHealAmount(caster, 1f);
        }

        public static float ResolveHealAmount(RuntimeHero caster, float powerMultiplier)
        {
            if (caster == null)
            {
                return 0f;
            }

            return caster.AttackPower * powerMultiplier;
        }
    }
}
