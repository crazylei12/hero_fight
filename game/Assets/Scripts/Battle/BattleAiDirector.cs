using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleAiDirector
    {
        private const float ThreatRetreatWindowSeconds = 0.75f;
        private const float ThreatRetreatTriggerRangeBuffer = 0.4f;
        private const float ThreatRetreatReleaseRangeBuffer = 0.2f;
        private const float ThreatRetreatMinimumAttackCooldownSeconds = 0.15f;
        private const float RangedThreatUnsafeDistanceFactor = 0.45f;
        private const float RangedThreatUnsafeDistanceMinimum = 1.75f;

        public static RuntimeHero SelectPreferredEnemyTarget(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            return actor.Definition.heroClass switch
            {
                HeroClass.Assassin => FindAssassinTarget(heroes, actor, maxRange),
                HeroClass.Marksman => FindLowestHealthEnemy(heroes, actor, maxRange),
                HeroClass.Mage => FindClusteredEnemy(heroes, actor, maxRange) ?? FindNearestEnemy(heroes, actor, maxRange),
                _ => FindNearestEnemy(heroes, actor, maxRange),
            };
        }

        public static RuntimeHero SelectNearestEnemyTarget(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            return FindNearestEnemy(heroes, actor, maxRange);
        }

        public static RuntimeHero SelectPreferredAllyTarget(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange, bool allowHealthyFallback = false)
        {
            return FindLowestHealthAlly(heroes, actor, maxRange, allowHealthyFallback);
        }

        public static float GetDesiredCombatRange(RuntimeHero actor)
        {
            var baseRange = actor.AttackRange;
            return actor.Definition.heroClass switch
            {
                HeroClass.Marksman => baseRange * 0.92f,
                HeroClass.Mage => baseRange * 0.9f,
                HeroClass.Support => Mathf.Max(3f, baseRange * 0.85f),
                HeroClass.Assassin => Mathf.Min(baseRange, 1.1f),
                _ => baseRange,
            };
        }

        public static Vector3 GetIdleAdvanceDirection(RuntimeHero actor)
        {
            if (actor.Definition.heroClass == HeroClass.Support)
            {
                return actor.Side == TeamSide.Blue ? new Vector3(0.6f, 0f, 0f) : new Vector3(-0.6f, 0f, 0f);
            }

            return actor.Side == TeamSide.Blue ? Vector3.right : Vector3.left;
        }

        public static bool ShouldRetreatFromRecentThreat(RuntimeHero actor, RuntimeHero combatTarget, float elapsedTimeSeconds, out RuntimeHero threat)
        {
            threat = null;
            if (actor?.Definition?.basicAttack == null || combatTarget == null || combatTarget.IsDead)
            {
                return false;
            }

            if (!actor.Definition.basicAttack.usesProjectile || actor.AttackCooldownRemainingSeconds <= ThreatRetreatMinimumAttackCooldownSeconds)
            {
                return false;
            }

            if (!actor.TryGetRecentThreat(elapsedTimeSeconds, ThreatRetreatWindowSeconds, out threat))
            {
                return false;
            }

            var desiredRange = GetDesiredCombatRange(actor);
            var threatDistance = Vector3.Distance(actor.CurrentPosition, threat.CurrentPosition);
            if (!IsThreatEligibleForRetreat(actor, threat, threatDistance, desiredRange))
            {
                return false;
            }

            var triggerDistance = Mathf.Max(0.75f, desiredRange - ThreatRetreatTriggerRangeBuffer);
            var releaseDistance = desiredRange + ThreatRetreatReleaseRangeBuffer;
            return actor.IsRetreatingFromThreat(threat)
                ? threatDistance < releaseDistance
                : threatDistance < triggerDistance;
        }

        public static Vector3 GetThreatRetreatDirection(RuntimeHero actor, RuntimeHero threat)
        {
            if (actor == null || threat == null)
            {
                return Vector3.zero;
            }

            var offset = actor.CurrentPosition - threat.CurrentPosition;
            if (offset.sqrMagnitude > Mathf.Epsilon)
            {
                return offset.normalized;
            }

            return actor.Side == TeamSide.Blue ? Vector3.left : Vector3.right;
        }

        private static bool IsThreatEligibleForRetreat(RuntimeHero actor, RuntimeHero threat, float threatDistance, float desiredRange)
        {
            if (actor?.Definition?.basicAttack == null || threat?.Definition?.basicAttack == null)
            {
                return false;
            }

            if (!threat.Definition.basicAttack.usesProjectile)
            {
                return true;
            }

            var unsafeDistance = Mathf.Max(
                RangedThreatUnsafeDistanceMinimum,
                desiredRange * RangedThreatUnsafeDistanceFactor);
            return threatDistance < unsafeDistance;
        }

        private static RuntimeHero FindNearestEnemy(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            RuntimeHero best = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (candidate.IsDead || candidate.Side == actor.Side || !candidate.CanBeDirectTargeted)
                {
                    continue;
                }

                var distance = Vector3.Distance(actor.CurrentPosition, candidate.CurrentPosition);
                if (distance > maxRange || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        private static RuntimeHero FindLowestHealthEnemy(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            RuntimeHero best = null;
            var bestRatio = float.MaxValue;
            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (candidate.IsDead || candidate.Side == actor.Side || !candidate.CanBeDirectTargeted)
                {
                    continue;
                }

                var distance = Vector3.Distance(actor.CurrentPosition, candidate.CurrentPosition);
                if (distance > maxRange)
                {
                    continue;
                }

                var ratio = candidate.MaxHealth > 0f ? candidate.CurrentHealth / candidate.MaxHealth : 1f;
                if (ratio >= bestRatio)
                {
                    continue;
                }

                bestRatio = ratio;
                best = candidate;
            }

            return best;
        }

        private static RuntimeHero FindLowestHealthAlly(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange, bool allowHealthyFallback)
        {
            RuntimeHero bestInjured = null;
            var bestInjuredRatio = 1f;
            RuntimeHero nearestHealthyAlly = null;
            var nearestHealthyDistance = float.MaxValue;
            RuntimeHero selfTarget = null;

            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (candidate.IsDead || candidate.Side != actor.Side)
                {
                    continue;
                }

                if (!candidate.CanBeDirectTargeted)
                {
                    continue;
                }

                var distance = Vector3.Distance(actor.CurrentPosition, candidate.CurrentPosition);
                if (distance > maxRange)
                {
                    continue;
                }

                var ratio = candidate.MaxHealth > 0f ? candidate.CurrentHealth / candidate.MaxHealth : 1f;

                if (candidate == actor)
                {
                    selfTarget = candidate;
                }

                if (ratio < bestInjuredRatio)
                {
                    bestInjuredRatio = ratio;
                    bestInjured = candidate;
                }

                if (!allowHealthyFallback || candidate == actor || distance >= nearestHealthyDistance)
                {
                    continue;
                }

                nearestHealthyDistance = distance;
                nearestHealthyAlly = candidate;
            }

            return bestInjured ?? (allowHealthyFallback ? nearestHealthyAlly ?? selfTarget : null);
        }

        private static RuntimeHero FindAssassinTarget(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            RuntimeHero best = null;
            var bestScore = float.MinValue;

            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (candidate.IsDead || candidate.Side == actor.Side || !candidate.CanBeDirectTargeted)
                {
                    continue;
                }

                var distance = Vector3.Distance(actor.CurrentPosition, candidate.CurrentPosition);
                if (distance > maxRange)
                {
                    continue;
                }

                var ratio = candidate.MaxHealth > 0f ? candidate.CurrentHealth / candidate.MaxHealth : 1f;
                var rearlineBonus = candidate.Definition.heroClass is HeroClass.Mage or HeroClass.Support or HeroClass.Marksman ? 0.5f : 0f;
                var score = rearlineBonus + (1f - ratio) - (distance * 0.03f);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                best = candidate;
            }

            return best ?? FindNearestEnemy(heroes, actor, maxRange);
        }

        private static RuntimeHero FindClusteredEnemy(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            RuntimeHero best = null;
            var bestCount = 0;
            var radius = actor.Definition.ultimateSkill != null ? Mathf.Max(2f, actor.Definition.ultimateSkill.areaRadius) : 2f;

            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (candidate.IsDead || candidate.Side == actor.Side || !candidate.CanBeDirectTargeted)
                {
                    continue;
                }

                var distance = Vector3.Distance(actor.CurrentPosition, candidate.CurrentPosition);
                if (distance > maxRange)
                {
                    continue;
                }

                var count = 0;
                for (var j = 0; j < heroes.Count; j++)
                {
                    var other = heroes[j];
                    if (other.IsDead || other.Side == actor.Side)
                    {
                        continue;
                    }

                    if (Vector3.Distance(candidate.CurrentPosition, other.CurrentPosition) <= radius)
                    {
                        count++;
                    }
                }

                if (count <= bestCount)
                {
                    continue;
                }

                bestCount = count;
                best = candidate;
            }

            return best;
        }
    }
}
