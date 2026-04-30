using Fight.Data;
using Fight.Heroes;

namespace Fight.Core
{
    public static class HealResolver
    {
        public static float ResolveHealAmount(RuntimeHero caster, SkillData skillData)
        {
            return ResolveHealAmount(caster, caster, skillData);
        }

        public static float ResolveHealAmount(RuntimeHero caster, RuntimeHero target, SkillData skillData)
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
                        return ResolveHealAmount(caster, target, effect);
                    }
                }
            }

            return ResolveHealAmount(caster, 1f);
        }

        public static float ResolveHealAmount(RuntimeHero caster, RuntimeHero target, SkillEffectData effect)
        {
            if (effect == null)
            {
                return 0f;
            }

            return ResolveHealAmount(caster, target, effect.powerMultiplier, effect.targetMaxHealthMultiplier);
        }

        public static float ResolveHealAmount(RuntimeHero caster, float powerMultiplier)
        {
            return ResolveHealAmount(caster, null, powerMultiplier, 0f);
        }

        public static float ResolveHealAmount(RuntimeHero caster, RuntimeHero target, float powerMultiplier, float targetMaxHealthMultiplier)
        {
            var amount = 0f;
            if (caster == null)
            {
                return amount;
            }

            amount += caster.AttackPower * UnityEngine.Mathf.Max(0f, powerMultiplier);
            if (target != null)
            {
                amount += target.MaxHealth * UnityEngine.Mathf.Max(0f, targetMaxHealthMultiplier);
            }

            return amount;
        }
    }
}
