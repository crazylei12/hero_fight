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

        public static RuntimeHero SelectBackmostEnemyTarget(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            return FindBackmostEnemy(heroes, actor, maxRange);
        }

        public static RuntimeHero SelectHighestDamageEnemyTarget(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            return FindHighestDamageEnemy(heroes, actor, maxRange);
        }

        public static RuntimeHero SelectEnemyTargetByHeroClass(
            IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero actor,
            float maxRange,
            HeroClass preferredHeroClass)
        {
            return FindNearestEnemyByHeroClass(heroes, actor, maxRange, preferredHeroClass);
        }

        public static RuntimeHero SelectLowestHealthRangedAllyTarget(
            IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero actor,
            float maxRange,
            bool allowHealthyFallback = false)
        {
            return FindLowestHealthRangedAlly(heroes, actor, maxRange, allowHealthyFallback);
        }

        public static RuntimeHero SelectThreatenedRangedAllyTarget(
            IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero actor,
            float maxRange,
            float threatRadius,
            int requiredThreatCount = 1)
        {
            return FindThreatenedRangedAlly(heroes, actor, maxRange, threatRadius, requiredThreatCount);
        }

        public static RuntimeHero SelectThreateningEnemyNearRangedAllyTarget(
            IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero actor,
            float threatRadius)
        {
            return FindThreateningEnemyNearRangedAlly(heroes, actor, threatRadius);
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

        private static RuntimeHero FindHighestDamageEnemy(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            RuntimeHero best = null;
            var highestDamageDealt = float.MinValue;
            var lowestCurrentHealth = float.MaxValue;
            var nearestDistance = float.MaxValue;

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

                if (!IsBetterHighestDamageEnemyCandidate(
                        candidate.DamageDealt,
                        candidate.CurrentHealth,
                        distance,
                        highestDamageDealt,
                        lowestCurrentHealth,
                        nearestDistance))
                {
                    continue;
                }

                highestDamageDealt = candidate.DamageDealt;
                lowestCurrentHealth = candidate.CurrentHealth;
                nearestDistance = distance;
                best = candidate;
            }

            return best;
        }

        private static RuntimeHero FindLowestHealthAlly(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange, bool allowHealthyFallback)
        {
            RuntimeHero bestInjured = null;
            var lowestInjuredHealth = float.MaxValue;
            var lowestInjuredRatio = float.MaxValue;
            var bestInjuredDistance = float.MaxValue;
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
                var isInjured = ratio < 1f - Mathf.Epsilon;

                if (candidate == actor)
                {
                    selfTarget = candidate;
                }

                if (isInjured && IsBetterInjuredAllyCandidate(candidate.CurrentHealth, ratio, distance, lowestInjuredHealth, lowestInjuredRatio, bestInjuredDistance))
                {
                    lowestInjuredHealth = candidate.CurrentHealth;
                    lowestInjuredRatio = ratio;
                    bestInjuredDistance = distance;
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

        private static RuntimeHero FindLowestHealthRangedAlly(
            IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero actor,
            float maxRange,
            bool allowHealthyFallback)
        {
            RuntimeHero bestInjured = null;
            var lowestInjuredHealth = float.MaxValue;
            var lowestInjuredRatio = float.MaxValue;
            var bestInjuredDistance = float.MaxValue;
            RuntimeHero nearestHealthyAlly = null;
            var nearestHealthyDistance = float.MaxValue;

            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (!IsValidRangedAllyCandidate(candidate, actor, maxRange))
                {
                    continue;
                }

                var distance = Vector3.Distance(actor.CurrentPosition, candidate.CurrentPosition);
                var ratio = candidate.MaxHealth > 0f ? candidate.CurrentHealth / candidate.MaxHealth : 1f;
                var isInjured = ratio < 1f - Mathf.Epsilon;

                if (isInjured && IsBetterInjuredAllyCandidate(candidate.CurrentHealth, ratio, distance, lowestInjuredHealth, lowestInjuredRatio, bestInjuredDistance))
                {
                    lowestInjuredHealth = candidate.CurrentHealth;
                    lowestInjuredRatio = ratio;
                    bestInjuredDistance = distance;
                    bestInjured = candidate;
                }

                if (!allowHealthyFallback || isInjured || distance >= nearestHealthyDistance)
                {
                    continue;
                }

                nearestHealthyDistance = distance;
                nearestHealthyAlly = candidate;
            }

            return bestInjured ?? nearestHealthyAlly;
        }

        private static RuntimeHero FindThreatenedRangedAlly(
            IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero actor,
            float maxRange,
            float threatRadius,
            int requiredThreatCount)
        {
            RuntimeHero best = null;
            var bestThreatCount = 0;
            var bestHealthRatio = float.MaxValue;
            var bestCurrentHealth = float.MaxValue;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (!IsValidRangedAllyCandidate(candidate, actor, maxRange))
                {
                    continue;
                }

                var threatCount = CountThreateningEnemiesNearHero(heroes, candidate, threatRadius);
                if (threatCount < Mathf.Max(1, requiredThreatCount))
                {
                    continue;
                }

                var healthRatio = candidate.MaxHealth > 0f ? candidate.CurrentHealth / candidate.MaxHealth : 1f;
                var distance = Vector3.Distance(actor.CurrentPosition, candidate.CurrentPosition);
                if (!IsBetterThreatenedRangedAllyCandidate(
                        threatCount,
                        candidate.CurrentHealth,
                        healthRatio,
                        distance,
                        bestThreatCount,
                        bestCurrentHealth,
                        bestHealthRatio,
                        bestDistance))
                {
                    continue;
                }

                bestThreatCount = threatCount;
                bestCurrentHealth = candidate.CurrentHealth;
                bestHealthRatio = healthRatio;
                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        private static RuntimeHero FindThreateningEnemyNearRangedAlly(
            IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero actor,
            float threatRadius)
        {
            RuntimeHero bestEnemy = null;
            var bestThreatDistance = float.MaxValue;
            var lowestEnemyHealth = float.MaxValue;
            var nearestActorDistance = float.MaxValue;

            for (var i = 0; i < heroes.Count; i++)
            {
                var enemy = heroes[i];
                if (enemy == null
                    || enemy.IsDead
                    || enemy.Side == actor.Side
                    || !enemy.CanBeDirectTargeted
                    || !IsBacklineThreatCandidate(enemy))
                {
                    continue;
                }

                if (!TryGetNearestThreatenedRangedAllyDistance(heroes, actor, enemy, threatRadius, out var threatenedDistance))
                {
                    continue;
                }

                var actorDistance = Vector3.Distance(actor.CurrentPosition, enemy.CurrentPosition);
                if (!IsBetterThreateningEnemyCandidate(
                        threatenedDistance,
                        enemy.CurrentHealth,
                        actorDistance,
                        bestThreatDistance,
                        lowestEnemyHealth,
                        nearestActorDistance))
                {
                    continue;
                }

                bestThreatDistance = threatenedDistance;
                lowestEnemyHealth = enemy.CurrentHealth;
                nearestActorDistance = actorDistance;
                bestEnemy = enemy;
            }

            return bestEnemy;
        }

        private static bool IsBetterInjuredAllyCandidate(
            float currentHealth,
            float healthRatio,
            float distance,
            float bestCurrentHealth,
            float bestHealthRatio,
            float bestDistance)
        {
            if (currentHealth < bestCurrentHealth - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(currentHealth - bestCurrentHealth) > Mathf.Epsilon)
            {
                return false;
            }

            if (healthRatio < bestHealthRatio - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(healthRatio - bestHealthRatio) > Mathf.Epsilon)
            {
                return false;
            }

            return distance < bestDistance;
        }

        private static RuntimeHero FindAssassinTarget(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            return FindBackmostEnemy(heroes, actor, maxRange) ?? FindNearestEnemy(heroes, actor, maxRange);
        }

        private static RuntimeHero FindNearestEnemyByHeroClass(
            IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero actor,
            float maxRange,
            HeroClass preferredHeroClass)
        {
            RuntimeHero best = null;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (candidate == null
                    || candidate.IsDead
                    || candidate.Side == actor.Side
                    || !candidate.CanBeDirectTargeted
                    || candidate.Definition == null
                    || candidate.Definition.heroClass != preferredHeroClass)
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

        private static bool TryGetNearestThreatenedRangedAllyDistance(
            IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero actor,
            RuntimeHero enemy,
            float threatRadius,
            out float threatenedDistance)
        {
            threatenedDistance = float.MaxValue;
            if (heroes == null || actor == null || enemy == null)
            {
                return false;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                var ally = heroes[i];
                if (ally == null
                    || ally.IsDead
                    || ally.Side != actor.Side
                    || !ally.CanBeDirectTargeted
                    || !IsRangedHero(ally))
                {
                    continue;
                }

                var distance = Vector3.Distance(enemy.CurrentPosition, ally.CurrentPosition);
                if (distance > threatRadius || distance >= threatenedDistance)
                {
                    continue;
                }

                threatenedDistance = distance;
            }

            return threatenedDistance < float.MaxValue;
        }

        private static RuntimeHero FindBackmostEnemy(IReadOnlyList<RuntimeHero> heroes, RuntimeHero actor, float maxRange)
        {
            RuntimeHero best = null;
            var lowestSpawnDistance = float.MaxValue;
            var lowestHealthRatio = float.MaxValue;
            var lowestCurrentHealth = float.MaxValue;

            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (candidate.IsDead || candidate.Side == actor.Side || !candidate.CanBeDirectTargeted)
                {
                    continue;
                }

                var distanceToActor = Vector3.Distance(actor.CurrentPosition, candidate.CurrentPosition);
                if (distanceToActor > maxRange)
                {
                    continue;
                }

                var spawnDistance = Vector3.Distance(candidate.CurrentPosition, candidate.SpawnPosition);
                var healthRatio = candidate.MaxHealth > 0f ? candidate.CurrentHealth / candidate.MaxHealth : 1f;
                var currentHealth = candidate.CurrentHealth;
                if (!IsBetterBackmostEnemyCandidate(
                        spawnDistance,
                        healthRatio,
                        currentHealth,
                        lowestSpawnDistance,
                        lowestHealthRatio,
                        lowestCurrentHealth))
                {
                    continue;
                }

                lowestSpawnDistance = spawnDistance;
                lowestHealthRatio = healthRatio;
                lowestCurrentHealth = currentHealth;
                best = candidate;
            }

            return best;
        }

        private static bool IsBetterBackmostEnemyCandidate(
            float spawnDistance,
            float healthRatio,
            float currentHealth,
            float bestSpawnDistance,
            float bestHealthRatio,
            float bestCurrentHealth)
        {
            if (spawnDistance < bestSpawnDistance - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(spawnDistance - bestSpawnDistance) > Mathf.Epsilon)
            {
                return false;
            }

            if (healthRatio < bestHealthRatio - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(healthRatio - bestHealthRatio) > Mathf.Epsilon)
            {
                return false;
            }

            return currentHealth < bestCurrentHealth - Mathf.Epsilon;
        }

        private static bool IsBetterHighestDamageEnemyCandidate(
            float damageDealt,
            float currentHealth,
            float distance,
            float bestDamageDealt,
            float bestCurrentHealth,
            float bestDistance)
        {
            if (damageDealt > bestDamageDealt + Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(damageDealt - bestDamageDealt) > Mathf.Epsilon)
            {
                return false;
            }

            if (currentHealth < bestCurrentHealth - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(currentHealth - bestCurrentHealth) > Mathf.Epsilon)
            {
                return false;
            }

            return distance < bestDistance;
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

        private static int CountThreateningEnemiesNearHero(IReadOnlyList<RuntimeHero> heroes, RuntimeHero protectedHero, float threatRadius)
        {
            if (heroes == null || protectedHero == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < heroes.Count; i++)
            {
                var enemy = heroes[i];
                if (enemy == null
                    || enemy.IsDead
                    || enemy.Side == protectedHero.Side
                    || !IsBacklineThreatCandidate(enemy))
                {
                    continue;
                }

                if (Vector3.Distance(enemy.CurrentPosition, protectedHero.CurrentPosition) <= threatRadius)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsValidRangedAllyCandidate(RuntimeHero candidate, RuntimeHero actor, float maxRange)
        {
            if (candidate == null
                || actor == null
                || candidate.IsDead
                || candidate.Side != actor.Side
                || !candidate.CanBeDirectTargeted
                || !IsRangedHero(candidate))
            {
                return false;
            }

            return Vector3.Distance(actor.CurrentPosition, candidate.CurrentPosition) <= maxRange;
        }

        private static bool IsRangedHero(RuntimeHero hero)
        {
            if (hero?.Definition == null)
            {
                return false;
            }

            var tags = hero.Definition.tags;
            if (tags != null && tags.Contains(HeroTag.Ranged))
            {
                return true;
            }

            return hero.Definition.basicAttack != null && hero.Definition.basicAttack.usesProjectile;
        }

        private static bool IsBacklineThreatCandidate(RuntimeHero hero)
        {
            if (hero?.Definition == null)
            {
                return false;
            }

            var tags = hero.Definition.tags;
            if (tags != null && tags.Contains(HeroTag.Melee))
            {
                return true;
            }

            return hero.Definition.heroClass == HeroClass.Assassin
                || hero.Definition.heroClass == HeroClass.Tank;
        }

        private static bool IsBetterThreatenedRangedAllyCandidate(
            int threatCount,
            float currentHealth,
            float healthRatio,
            float distance,
            int bestThreatCount,
            float bestCurrentHealth,
            float bestHealthRatio,
            float bestDistance)
        {
            if (threatCount > bestThreatCount)
            {
                return true;
            }

            if (threatCount != bestThreatCount)
            {
                return false;
            }

            if (currentHealth < bestCurrentHealth - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(currentHealth - bestCurrentHealth) > Mathf.Epsilon)
            {
                return false;
            }

            if (healthRatio < bestHealthRatio - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(healthRatio - bestHealthRatio) > Mathf.Epsilon)
            {
                return false;
            }

            return distance < bestDistance;
        }

        private static bool IsBetterThreateningEnemyCandidate(
            float threatDistance,
            float currentHealth,
            float actorDistance,
            float bestThreatDistance,
            float bestCurrentHealth,
            float bestActorDistance)
        {
            if (threatDistance < bestThreatDistance - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(threatDistance - bestThreatDistance) > Mathf.Epsilon)
            {
                return false;
            }

            if (currentHealth < bestCurrentHealth - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(currentHealth - bestCurrentHealth) > Mathf.Epsilon)
            {
                return false;
            }

            return actorDistance < bestActorDistance;
        }
    }
}
