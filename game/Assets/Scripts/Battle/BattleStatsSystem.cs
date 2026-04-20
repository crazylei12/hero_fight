using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleStatsSystem
    {
        private const float AssistWindowSeconds = 8f;

        public static void RecordDamageContribution(BattleContext context, RuntimeHero attacker, RuntimeHero target, float amount)
        {
            if (target == null || amount <= 0f)
            {
                return;
            }

            target.RecordDamageTaken(amount);
            attacker?.RecordDamage(amount);
            RegisterHostileContribution(context, attacker, target);
        }

        public static void RecordHealingContribution(BattleContext context, RuntimeHero source, RuntimeHero target, float amount)
        {
            if (target == null || amount <= 0f)
            {
                return;
            }

            source?.RecordHealing(amount);
            RegisterSupportContribution(context, source, target);
        }

        public static void RecordShieldContribution(BattleContext context, RuntimeHero source, RuntimeHero target, float amount)
        {
            if (target == null || amount <= 0f)
            {
                return;
            }

            source?.RecordShielding(amount);
            RegisterSupportContribution(context, source, target);
        }

        public static void RecordStatusContribution(BattleContext context, RuntimeHero source, RuntimeHero target, StatusEffectData status)
        {
            if (source == null || target == null || status == null)
            {
                return;
            }

            if (source.Side != target.Side)
            {
                if (IsHostileAssistStatus(status))
                {
                    RegisterHostileContribution(context, source, target);
                }

                return;
            }

            if (IsSupportAssistStatus(status))
            {
                RegisterSupportContribution(context, source, target);
            }
        }

        public static void RecordForcedMovementContribution(BattleContext context, RuntimeHero source, RuntimeHero target)
        {
            if (source == null || target == null || source.Side == target.Side)
            {
                return;
            }

            RegisterHostileContribution(context, source, target);
        }

        public static void ResolveAssists(BattleContext context, RuntimeHero victim, RuntimeHero killer)
        {
            if (victim == null)
            {
                return;
            }

            if (context == null || killer == null || killer.Side == victim.Side)
            {
                victim.ClearContributionHistory();
                return;
            }

            var battleTimeSeconds = GetBattleTimeSeconds(context);
            var offensiveContributors = new HashSet<RuntimeHero>();
            victim.CollectRecentHostileContributors(
                killer.Side,
                battleTimeSeconds,
                AssistWindowSeconds,
                offensiveContributors);

            var assisters = new HashSet<RuntimeHero>();
            foreach (var contributor in offensiveContributors)
            {
                if (contributor != null && contributor != killer && contributor != victim)
                {
                    assisters.Add(contributor);
                }
            }

            var supportRecipients = new HashSet<RuntimeHero>(offensiveContributors)
            {
                killer,
            };

            foreach (var recipient in supportRecipients)
            {
                recipient?.CollectRecentSupportContributors(
                    killer.Side,
                    battleTimeSeconds,
                    AssistWindowSeconds,
                    assisters);
            }

            assisters.Remove(killer);
            assisters.Remove(victim);
            foreach (var assister in assisters)
            {
                assister?.MarkAssist();
            }

            victim.ClearContributionHistory();
        }

        private static void RegisterHostileContribution(BattleContext context, RuntimeHero contributor, RuntimeHero target)
        {
            if (contributor == null || target == null || contributor.Side == target.Side)
            {
                return;
            }

            target.RegisterHostileContribution(contributor, GetBattleTimeSeconds(context));
        }

        private static void RegisterSupportContribution(BattleContext context, RuntimeHero contributor, RuntimeHero target)
        {
            if (contributor == null || target == null || contributor.Side != target.Side)
            {
                return;
            }

            target.RegisterSupportContribution(contributor, GetBattleTimeSeconds(context));
        }

        private static float GetBattleTimeSeconds(BattleContext context)
        {
            return context?.Clock != null ? Mathf.Max(0f, context.Clock.ElapsedTimeSeconds) : 0f;
        }

        private static bool IsHostileAssistStatus(StatusEffectData status)
        {
            if (status == null)
            {
                return false;
            }

            switch (status.effectType)
            {
                case StatusEffectType.DamageOverTime:
                case StatusEffectType.Stun:
                case StatusEffectType.KnockUp:
                case StatusEffectType.Taunt:
                    return true;
                case StatusEffectType.AttackPowerModifier:
                case StatusEffectType.DefenseModifier:
                case StatusEffectType.AttackSpeedModifier:
                case StatusEffectType.MoveSpeedModifier:
                case StatusEffectType.MaxHealthModifier:
                case StatusEffectType.CriticalChanceModifier:
                case StatusEffectType.CriticalDamageModifier:
                case StatusEffectType.AttackRangeModifier:
                    return status.magnitude < -Mathf.Epsilon;
                default:
                    return false;
            }
        }

        private static bool IsSupportAssistStatus(StatusEffectData status)
        {
            if (status == null)
            {
                return false;
            }

            switch (status.effectType)
            {
                case StatusEffectType.HealOverTime:
                case StatusEffectType.Shield:
                case StatusEffectType.Invulnerable:
                case StatusEffectType.Untargetable:
                case StatusEffectType.DamageShare:
                    return true;
                case StatusEffectType.AttackPowerModifier:
                case StatusEffectType.DefenseModifier:
                case StatusEffectType.AttackSpeedModifier:
                case StatusEffectType.MoveSpeedModifier:
                case StatusEffectType.MaxHealthModifier:
                case StatusEffectType.CriticalChanceModifier:
                case StatusEffectType.CriticalDamageModifier:
                case StatusEffectType.AttackRangeModifier:
                    return status.magnitude > Mathf.Epsilon;
                default:
                    return false;
            }
        }
    }
}
