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

            var contributionSource = ResolveContributionSource(attacker);
            target.RecordDamageTaken(amount);
            contributionSource?.RecordDamage(amount);
            RegisterHostileContribution(context, contributionSource, target);
        }

        public static void RecordHealingContribution(BattleContext context, RuntimeHero source, RuntimeHero target, float amount)
        {
            if (target == null || amount <= 0f)
            {
                return;
            }

            var contributionSource = ResolveContributionSource(source);
            contributionSource?.RecordHealing(amount);
            RegisterSupportContribution(context, contributionSource, target);
        }

        public static void RecordDirectHostileDamageContribution(BattleContext context, RuntimeHero source, RuntimeHero target)
        {
            var contributionSource = ResolveContributionSource(source);
            if (contributionSource == null || target == null || contributionSource.Side == target.Side)
            {
                return;
            }

            target.RegisterDirectHostileDamageContribution(contributionSource, GetBattleTimeSeconds(context));
        }

        public static void RecordShieldContribution(BattleContext context, RuntimeHero source, RuntimeHero target, float amount)
        {
            if (target == null || amount <= 0f)
            {
                return;
            }

            var contributionSource = ResolveContributionSource(source);
            contributionSource?.RecordShielding(amount);
            RegisterSupportContribution(context, contributionSource, target);
        }

        public static void RecordStatusContribution(BattleContext context, RuntimeHero source, RuntimeHero target, StatusEffectData status)
        {
            var contributionSource = ResolveContributionSource(source);
            if (contributionSource == null || target == null || status == null)
            {
                return;
            }

            if (contributionSource.Side != target.Side)
            {
                if (IsHostileAssistStatus(status))
                {
                    RegisterHostileContribution(context, contributionSource, target);
                }

                return;
            }

            if (IsSupportAssistStatus(status))
            {
                RegisterSupportContribution(context, contributionSource, target);
            }
        }

        public static void RecordForcedMovementContribution(BattleContext context, RuntimeHero source, RuntimeHero target)
        {
            var contributionSource = ResolveContributionSource(source);
            if (contributionSource == null || target == null || contributionSource.Side == target.Side)
            {
                return;
            }

            RegisterHostileContribution(context, contributionSource, target);
        }

        public static List<RuntimeHero> ResolveKillParticipants(BattleContext context, RuntimeHero victim, RuntimeHero killer)
        {
            var participants = new List<RuntimeHero>();
            if (victim == null)
            {
                return participants;
            }

            killer = ResolveContributionSource(killer);
            if (context == null || killer == null || killer.Side == victim.Side)
            {
                victim.ClearContributionHistory();
                return participants;
            }

            participants.Add(killer);

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
                if (assister != null)
                {
                    participants.Add(assister);
                }
            }

            victim.ClearContributionHistory();
            return participants;
        }

        public static void ResolveAssists(BattleContext context, RuntimeHero victim, RuntimeHero killer)
        {
            ResolveKillParticipants(context, victim, killer);
        }

        private static void RegisterHostileContribution(BattleContext context, RuntimeHero contributor, RuntimeHero target)
        {
            contributor = ResolveContributionSource(contributor);
            if (contributor == null || target == null || contributor.Side == target.Side)
            {
                return;
            }

            target.RegisterHostileContribution(contributor, GetBattleTimeSeconds(context));
        }

        private static void RegisterSupportContribution(BattleContext context, RuntimeHero contributor, RuntimeHero target)
        {
            contributor = ResolveContributionSource(contributor);
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

        private static RuntimeHero ResolveContributionSource(RuntimeHero source)
        {
            return source != null && source.IsClone && source.CloneOwner != null
                ? source.CloneOwner
                : source;
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
                case StatusEffectType.HealTakenModifier:
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
                case StatusEffectType.DeathPrevent:
                    return true;
                case StatusEffectType.AttackPowerModifier:
                case StatusEffectType.DefenseModifier:
                case StatusEffectType.AttackSpeedModifier:
                case StatusEffectType.MoveSpeedModifier:
                case StatusEffectType.MaxHealthModifier:
                case StatusEffectType.CriticalChanceModifier:
                case StatusEffectType.CriticalDamageModifier:
                case StatusEffectType.AttackRangeModifier:
                case StatusEffectType.HealTakenModifier:
                    return status.magnitude > Mathf.Epsilon;
                default:
                    return false;
            }
        }
    }
}
