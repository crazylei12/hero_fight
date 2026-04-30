using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleCloneSystem
    {
        private const int SpawnSearchSteps = 12;

        public static int CreateClones(
            BattleContext context,
            RuntimeHero owner,
            SkillData sourceSkill,
            SkillEffectData effect,
            IReadOnlyList<RuntimeHero> sourceTargets)
        {
            if (context?.Heroes == null
                || owner == null
                || owner.IsDead
                || sourceSkill == null
                || effect == null
                || sourceTargets == null)
            {
                return 0;
            }

            var orderedSources = CollectCloneSources(owner, sourceTargets);
            if (orderedSources.Count <= 0)
            {
                return 0;
            }

            var cloneGroupKey = string.IsNullOrWhiteSpace(sourceSkill.skillId)
                ? sourceSkill.displayName ?? string.Empty
                : sourceSkill.skillId;
            var spawnLimit = Mathf.Min(Mathf.Max(1, effect.cloneSpawnCount), orderedSources.Count);
            var maxCount = Mathf.Max(0, effect.cloneMaxCount);
            var spawnedCount = 0;
            var reservedPositions = new List<Vector3>();

            for (var i = 0; i < spawnLimit; i++)
            {
                if (!EnsureCloneCapacity(context, owner, cloneGroupKey, maxCount, effect.cloneReplaceOldestWhenLimitReached))
                {
                    break;
                }

                var source = orderedSources[i];
                var sequence = context.NextCloneSequence();
                var spawnPosition = ResolveSpawnPosition(context, owner, source, Mathf.Max(0.1f, effect.cloneSpawnOffsetDistance), reservedPositions);
                reservedPositions.Add(spawnPosition);

                var clone = new RuntimeHero(
                    source.Definition,
                    owner.Side,
                    spawnPosition,
                    -1000 - sequence,
                    source.AthleteModifier);
                clone.ConfigureAsClone(
                    $"clone_{sequence:0000}_{owner.Side}_{source.Definition?.heroId}",
                    sequence,
                    owner,
                    source,
                    sourceSkill,
                    cloneGroupKey,
                    effect.cloneDurationSeconds,
                    effect.cloneMaxHealthMultiplier,
                    effect.cloneAttackPowerMultiplier,
                    effect.cloneDefenseMultiplier,
                    effect.cloneAttackSpeedMultiplier,
                    effect.cloneMoveSpeedMultiplier,
                    effect.cloneInitialActiveSkillDelaySeconds,
                    effect.cloneExpiresWhenOwnerDies);

                context.Heroes.Add(clone);
                context.EventBus?.Publish(new CloneUnitSpawnedEvent(clone, owner, source, sourceSkill));
                context.EventBus?.Publish(new UnitSpawnedEvent(clone));
                spawnedCount++;
            }

            return spawnedCount;
        }

        public static void Tick(BattleContext context)
        {
            if (context?.Heroes == null)
            {
                return;
            }

            for (var i = context.Heroes.Count - 1; i >= 0; i--)
            {
                var clone = context.Heroes[i];
                if (clone == null)
                {
                    context.Heroes.RemoveAt(i);
                    continue;
                }

                if (!clone.IsClone)
                {
                    continue;
                }

                if (clone.IsDead)
                {
                    RemoveCloneAt(context, i, CloneUnitRemovalReason.Killed);
                    continue;
                }

                if (clone.IsCloneExpired)
                {
                    RemoveCloneAt(context, i, CloneUnitRemovalReason.Expired);
                    continue;
                }

                if (clone.CloneExpiresWhenOwnerDies
                    && (clone.CloneOwner == null || clone.CloneOwner.IsDead))
                {
                    RemoveCloneAt(context, i, CloneUnitRemovalReason.OwnerUnavailable);
                }
            }
        }

        private static List<RuntimeHero> CollectCloneSources(RuntimeHero owner, IReadOnlyList<RuntimeHero> sourceTargets)
        {
            var results = new List<RuntimeHero>();
            var seenIds = new HashSet<string>();
            for (var i = 0; i < sourceTargets.Count; i++)
            {
                var candidate = sourceTargets[i];
                if (!CanCloneSource(owner, candidate))
                {
                    continue;
                }

                var runtimeId = candidate.RuntimeId ?? string.Empty;
                if (!seenIds.Add(runtimeId))
                {
                    continue;
                }

                results.Add(candidate);
            }

            results.Sort(CompareCloneSourceThreat);
            return results;
        }

        public static bool CanCloneSource(RuntimeHero owner, RuntimeHero candidate)
        {
            return owner != null
                && candidate != null
                && candidate.Definition != null
                && !candidate.IsDead
                && !candidate.IsClone
                && candidate.Side != owner.Side
                && candidate.CanBeDirectTargeted;
        }

        private static int CompareCloneSourceThreat(RuntimeHero left, RuntimeHero right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            var damageComparison = right.DamageDealt.CompareTo(left.DamageDealt);
            if (damageComparison != 0)
            {
                return damageComparison;
            }

            var attackPowerComparison = right.AttackPower.CompareTo(left.AttackPower);
            if (attackPowerComparison != 0)
            {
                return attackPowerComparison;
            }

            var healthComparison = right.CurrentHealth.CompareTo(left.CurrentHealth);
            if (healthComparison != 0)
            {
                return healthComparison;
            }

            return string.CompareOrdinal(left.RuntimeId, right.RuntimeId);
        }

        private static bool EnsureCloneCapacity(
            BattleContext context,
            RuntimeHero owner,
            string cloneGroupKey,
            int maxCount,
            bool replaceOldestWhenLimitReached)
        {
            if (maxCount <= 0)
            {
                return true;
            }

            while (CountOwnerClones(context, owner, cloneGroupKey) >= maxCount)
            {
                var oldestIndex = FindOldestCloneIndex(context, owner, cloneGroupKey);
                if (oldestIndex < 0)
                {
                    return true;
                }

                if (!replaceOldestWhenLimitReached)
                {
                    return false;
                }

                RemoveCloneAt(context, oldestIndex, CloneUnitRemovalReason.Replaced);
            }

            return true;
        }

        private static int CountOwnerClones(BattleContext context, RuntimeHero owner, string cloneGroupKey)
        {
            var count = 0;
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                if (IsCloneInGroup(context.Heroes[i], owner, cloneGroupKey))
                {
                    count++;
                }
            }

            return count;
        }

        private static int FindOldestCloneIndex(BattleContext context, RuntimeHero owner, string cloneGroupKey)
        {
            var bestIndex = -1;
            var bestSequence = int.MaxValue;
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var clone = context.Heroes[i];
                if (!IsCloneInGroup(clone, owner, cloneGroupKey) || clone.CloneSequence >= bestSequence)
                {
                    continue;
                }

                bestIndex = i;
                bestSequence = clone.CloneSequence;
            }

            return bestIndex;
        }

        private static bool IsCloneInGroup(RuntimeHero clone, RuntimeHero owner, string cloneGroupKey)
        {
            return clone != null
                && clone.IsClone
                && clone.CloneOwner == owner
                && string.Equals(clone.CloneGroupKey, cloneGroupKey ?? string.Empty, System.StringComparison.Ordinal);
        }

        private static void RemoveCloneAt(BattleContext context, int index, CloneUnitRemovalReason reason)
        {
            if (context?.Heroes == null || index < 0 || index >= context.Heroes.Count)
            {
                return;
            }

            var clone = context.Heroes[index];
            context.Heroes.RemoveAt(index);
            if (clone != null)
            {
                clone.ForceCloneExpired();
                context.EventBus?.Publish(new CloneUnitRemovedEvent(clone, reason));
            }
        }

        private static Vector3 ResolveSpawnPosition(
            BattleContext context,
            RuntimeHero owner,
            RuntimeHero source,
            float offsetDistance,
            IReadOnlyList<Vector3> reservedPositions)
        {
            var baseDirection = owner.CurrentPosition - source.CurrentPosition;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude <= 0.0001f)
            {
                baseDirection = owner.Side == TeamSide.Blue ? Vector3.left : Vector3.right;
            }

            baseDirection.Normalize();
            var sourcePosition = source.CurrentPosition;
            var bestPosition = Stage01ArenaSpec.ClampPosition(sourcePosition + baseDirection * offsetDistance);
            var bestScore = EvaluateSpawnPosition(context, bestPosition, reservedPositions);

            for (var i = 1; i <= SpawnSearchSteps; i++)
            {
                var angle = (360f / SpawnSearchSteps) * i;
                var direction = Quaternion.Euler(0f, angle, 0f) * baseDirection;
                var candidate = Stage01ArenaSpec.ClampPosition(sourcePosition + direction * offsetDistance);
                var score = EvaluateSpawnPosition(context, candidate, reservedPositions);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestPosition = candidate;
            }

            return bestPosition;
        }

        private static float EvaluateSpawnPosition(
            BattleContext context,
            Vector3 position,
            IReadOnlyList<Vector3> reservedPositions)
        {
            var nearestDistanceSqr = float.MaxValue;
            if (context?.Heroes != null)
            {
                for (var i = 0; i < context.Heroes.Count; i++)
                {
                    var hero = context.Heroes[i];
                    if (hero == null || hero.IsDead)
                    {
                        continue;
                    }

                    var distanceSqr = (hero.CurrentPosition - position).sqrMagnitude;
                    if (distanceSqr < nearestDistanceSqr)
                    {
                        nearestDistanceSqr = distanceSqr;
                    }
                }
            }

            if (reservedPositions != null)
            {
                for (var i = 0; i < reservedPositions.Count; i++)
                {
                    var distanceSqr = (reservedPositions[i] - position).sqrMagnitude;
                    if (distanceSqr < nearestDistanceSqr)
                    {
                        nearestDistanceSqr = distanceSqr;
                    }
                }
            }

            return nearestDistanceSqr;
        }
    }
}
