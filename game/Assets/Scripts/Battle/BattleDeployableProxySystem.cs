using System.Collections.Generic;
using Fight.Core;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleDeployableProxySystem
    {
        private static int deployableProxySequence;

        public static void Tick(BattleContext context, float deltaTime, IBattleSimulationCallbacks battleCallbacks)
        {
            if (context?.DeployableProxies == null)
            {
                return;
            }

            for (var i = context.DeployableProxies.Count - 1; i >= 0; i--)
            {
                var proxy = context.DeployableProxies[i];
                if (proxy == null)
                {
                    context.DeployableProxies.RemoveAt(i);
                    continue;
                }

                proxy.Tick(deltaTime);
                if (TryFireProximityExplosion(context, proxy, battleCallbacks))
                {
                    context.EventBus?.Publish(new DeployableProxyRemovedEvent(proxy, DeployableProxyRemovalReason.Triggered));
                    context.DeployableProxies.RemoveAt(i);
                    continue;
                }

                TryFirePeriodicProxyAttack(context, proxy, battleCallbacks);
                TryFirePeriodicProxyEffectPulse(context, proxy);
                if (proxy.IsExpired)
                {
                    context.EventBus?.Publish(new DeployableProxyRemovedEvent(proxy, DeployableProxyRemovalReason.Expired));
                    context.DeployableProxies.RemoveAt(i);
                }
            }
        }

        public static void CreateDeployableProxies(
            BattleContext context,
            RuntimeHero owner,
            SkillData sourceSkill,
            SkillEffectData effect,
            IReadOnlyList<RuntimeHero> anchorTargets,
            IBattleSimulationCallbacks battleCallbacks)
        {
            if (context?.DeployableProxies == null
                || owner == null
                || owner.IsDead
                || effect == null
                || effect.effectType != SkillEffectType.CreateDeployableProxy
                || anchorTargets == null
                || anchorTargets.Count <= 0)
            {
                return;
            }

            var maxProxyCount = Mathf.Max(0, effect.deployableProxyMaxCount);
            if (maxProxyCount <= 0)
            {
                return;
            }

            if (effect.deployableProxySpawnMode == DeployableProxySpawnMode.RandomForwardArea)
            {
                CreateRandomForwardDeployableProxies(context, owner, sourceSkill, effect, anchorTargets[0]);
                return;
            }

            for (var i = 0; i < anchorTargets.Count; i++)
            {
                var anchorTarget = anchorTargets[i];
                if (anchorTarget == null || anchorTarget.IsDead)
                {
                    continue;
                }

                if (!TryMakeRoomForProxy(context, owner, maxProxyCount, effect.deployableProxyReplaceOldestWhenLimitReached))
                {
                    break;
                }

                var spawnPosition = ResolveSpawnPosition(owner, anchorTarget, effect);
                var proxy = new RuntimeDeployableProxy(owner, sourceSkill, effect, spawnPosition, deployableProxySequence++);
                context.DeployableProxies.Add(proxy);
                context.EventBus?.Publish(new DeployableProxySpawnedEvent(proxy));

                if (effect.deployableProxyImmediateStrikeOnSpawn)
                {
                    ResolveProxyStrike(context, proxy, anchorTarget, battleCallbacks, DamageSourceKind.Skill, sourceSkill);
                }
            }
        }

        public static void TriggerOwnerBasicAttackProxies(
            BattleContext context,
            RuntimeHero owner,
            RuntimeHero preferredTarget,
            IBattleSimulationCallbacks battleCallbacks)
        {
            if (context?.DeployableProxies == null
                || owner == null
                || owner.IsDead
                || battleCallbacks == null)
            {
                return;
            }

            for (var i = 0; i < context.DeployableProxies.Count; i++)
            {
                var proxy = context.DeployableProxies[i];
                if (proxy == null
                    || proxy.IsExpired
                    || proxy.Owner != owner
                    || proxy.TriggerMode != DeployableProxyTriggerMode.OnOwnerBasicAttack)
                {
                    continue;
                }

                var strikeTarget = SelectStrikeTarget(context, proxy, preferredTarget);
                if (strikeTarget == null)
                {
                    continue;
                }

                ResolveProxyStrike(context, proxy, strikeTarget, battleCallbacks, DamageSourceKind.BasicAttack, null);
            }
        }

        private static bool TryMakeRoomForProxy(
            BattleContext context,
            RuntimeHero owner,
            int maxProxyCount,
            bool replaceOldestWhenLimitReached)
        {
            if (context?.DeployableProxies == null || owner == null || maxProxyCount <= 0)
            {
                return false;
            }

            while (CountOwnerProxies(context, owner) >= maxProxyCount)
            {
                if (!replaceOldestWhenLimitReached)
                {
                    return false;
                }

                if (!TryRemoveOldestOwnerProxy(context, owner))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountOwnerProxies(BattleContext context, RuntimeHero owner)
        {
            if (context?.DeployableProxies == null || owner == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < context.DeployableProxies.Count; i++)
            {
                var proxy = context.DeployableProxies[i];
                if (proxy != null && !proxy.IsExpired && proxy.Owner == owner)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryRemoveOldestOwnerProxy(BattleContext context, RuntimeHero owner)
        {
            if (context?.DeployableProxies == null || owner == null)
            {
                return false;
            }

            var oldestIndex = -1;
            var oldestSequence = int.MaxValue;
            for (var i = 0; i < context.DeployableProxies.Count; i++)
            {
                var proxy = context.DeployableProxies[i];
                if (proxy == null)
                {
                    oldestIndex = i;
                    break;
                }

                if (proxy.Owner != owner || proxy.IsExpired || proxy.SpawnSequence >= oldestSequence)
                {
                    continue;
                }

                oldestSequence = proxy.SpawnSequence;
                oldestIndex = i;
            }

            if (oldestIndex < 0)
            {
                return false;
            }

            var removedProxy = context.DeployableProxies[oldestIndex];
            if (removedProxy != null)
            {
                context.EventBus?.Publish(new DeployableProxyRemovedEvent(removedProxy, DeployableProxyRemovalReason.Replaced));
            }

            context.DeployableProxies.RemoveAt(oldestIndex);
            return true;
        }

        private static void CreateRandomForwardDeployableProxies(
            BattleContext context,
            RuntimeHero owner,
            SkillData sourceSkill,
            SkillEffectData effect,
            RuntimeHero anchorTarget)
        {
            var maxProxyCount = Mathf.Max(0, effect.deployableProxyMaxCount);
            var spawnCount = Mathf.Max(1, effect.deployableProxySpawnCount);
            var reservedPositions = new List<Vector3>();
            CollectOwnerProxyPositions(context, owner, reservedPositions);
            for (var i = 0; i < spawnCount; i++)
            {
                if (!TryMakeRoomForProxy(context, owner, maxProxyCount, effect.deployableProxyReplaceOldestWhenLimitReached))
                {
                    break;
                }

                var spawnPosition = ResolveRandomForwardSpawnPosition(context, owner, anchorTarget, effect, reservedPositions);
                reservedPositions.Add(spawnPosition);
                var proxy = new RuntimeDeployableProxy(owner, sourceSkill, effect, spawnPosition, deployableProxySequence++);
                context.DeployableProxies.Add(proxy);
                context.EventBus?.Publish(new DeployableProxySpawnedEvent(proxy));
            }
        }

        private static void CollectOwnerProxyPositions(BattleContext context, RuntimeHero owner, ICollection<Vector3> results)
        {
            if (context?.DeployableProxies == null || owner == null || results == null)
            {
                return;
            }

            for (var i = 0; i < context.DeployableProxies.Count; i++)
            {
                var proxy = context.DeployableProxies[i];
                if (proxy != null && !proxy.IsExpired && proxy.Owner == owner)
                {
                    results.Add(proxy.CurrentPosition);
                }
            }
        }

        private static Vector3 ResolveSpawnPosition(RuntimeHero owner, RuntimeHero anchorTarget, SkillEffectData effect)
        {
            if (anchorTarget == null)
            {
                return owner != null ? owner.CurrentPosition : Vector3.zero;
            }

            var targetPosition = anchorTarget.CurrentPosition;
            if (effect == null
                || effect.deployableProxySpawnMode == DeployableProxySpawnMode.AtTargetPosition
                || effect.deployableProxySpawnOffsetDistance <= Mathf.Epsilon)
            {
                return Stage01ArenaSpec.ClampPosition(targetPosition);
            }

            var direction = targetPosition - (owner != null ? owner.CurrentPosition : Vector3.zero);
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = owner != null && owner.Side == TeamSide.Blue ? Vector3.right : Vector3.left;
            }

            var spawnPosition = targetPosition - direction.normalized * effect.deployableProxySpawnOffsetDistance;
            return Stage01ArenaSpec.ClampPosition(spawnPosition);
        }

        private static Vector3 ResolveRandomForwardSpawnPosition(
            BattleContext context,
            RuntimeHero owner,
            RuntimeHero anchorTarget,
            SkillEffectData effect,
            IReadOnlyList<Vector3> reservedPositions)
        {
            var origin = owner != null ? owner.CurrentPosition : Vector3.zero;
            origin.y = 0f;
            var forward = ResolveForwardDirection(owner, anchorTarget);
            var lateral = new Vector3(-forward.z, 0f, forward.x);
            var minDistance = Mathf.Max(0f, effect != null ? effect.deployableProxyRandomForwardMinDistance : 0f);
            var maxDistance = Mathf.Max(minDistance, effect != null ? effect.deployableProxyRandomForwardMaxDistance : minDistance);
            var halfWidth = Mathf.Max(0f, effect != null ? effect.deployableProxyRandomForwardWidth * 0.5f : 0f);
            var minSpacing = Mathf.Max(0f, effect != null ? effect.deployableProxyRandomForwardMinSpacing : 0f);
            var random = context != null ? context.RandomService : null;
            var bestCandidate = Stage01ArenaSpec.ClampPosition(origin + forward * minDistance);

            const int maxAttempts = 18;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var distance = random != null ? random.Range(minDistance, maxDistance) : minDistance;
                var lateralOffset = halfWidth > Mathf.Epsilon && random != null
                    ? random.Range(-halfWidth, halfWidth)
                    : 0f;
                var candidate = Stage01ArenaSpec.ClampPosition(origin + forward * distance + lateral * lateralOffset);
                candidate.y = 0f;
                bestCandidate = candidate;
                if (!HasNearbyReservedPosition(reservedPositions, candidate, minSpacing))
                {
                    return candidate;
                }
            }

            return bestCandidate;
        }

        private static Vector3 ResolveForwardDirection(RuntimeHero owner, RuntimeHero anchorTarget)
        {
            var origin = owner != null ? owner.CurrentPosition : Vector3.zero;
            var targetPosition = anchorTarget != null && anchorTarget != owner
                ? anchorTarget.CurrentPosition
                : origin + (owner != null && owner.Side == TeamSide.Blue ? Vector3.right : Vector3.left);
            var direction = targetPosition - origin;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = owner != null && owner.Side == TeamSide.Blue ? Vector3.right : Vector3.left;
            }

            return direction.normalized;
        }

        private static bool HasNearbyReservedPosition(IReadOnlyList<Vector3> positions, Vector3 candidate, float minSpacing)
        {
            if (positions == null || minSpacing <= Mathf.Epsilon)
            {
                return false;
            }

            for (var i = 0; i < positions.Count; i++)
            {
                if (Vector3.Distance(positions[i], candidate) < minSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        private static RuntimeHero SelectStrikeTarget(BattleContext context, RuntimeDeployableProxy proxy, RuntimeHero preferredTarget)
        {
            if (context?.Heroes == null || proxy?.Owner == null)
            {
                return null;
            }

            var strikeRadius = proxy.StrikeRadius;
            if (strikeRadius <= Mathf.Epsilon)
            {
                return null;
            }

            if (IsValidStrikeTarget(proxy.Owner, preferredTarget)
                && Vector3.Distance(proxy.CurrentPosition, preferredTarget.CurrentPosition) <= strikeRadius)
            {
                return preferredTarget;
            }

            RuntimeHero bestTarget = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (!IsValidStrikeTarget(proxy.Owner, candidate))
                {
                    continue;
                }

                var distance = Vector3.Distance(proxy.CurrentPosition, candidate.CurrentPosition);
                if (distance > strikeRadius || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestTarget = candidate;
            }

            return bestTarget;
        }

        private static bool IsValidStrikeTarget(RuntimeHero owner, RuntimeHero target)
        {
            return owner != null
                && target != null
                && !target.IsDead
                && target.Side != owner.Side
                && target.CanBeDirectTargeted;
        }

        private static void ResolveProxyStrike(
            BattleContext context,
            RuntimeDeployableProxy proxy,
            RuntimeHero target,
            IBattleSimulationCallbacks battleCallbacks,
            DamageSourceKind sourceKind,
            SkillData sourceSkill)
        {
            if (context == null
                || proxy?.Owner == null
                || target == null
                || target.IsDead
                || !IsValidStrikeTarget(proxy.Owner, target)
                || battleCallbacks == null)
            {
                return;
            }

            var damageMultiplier = proxy.StrikePowerMultiplier;
            if (damageMultiplier <= Mathf.Epsilon)
            {
                return;
            }

            var damage = DamageResolver.ResolveDamage(
                proxy.Owner.AttackPower,
                proxy.Owner.CriticalChance,
                proxy.Owner.CriticalDamageMultiplier,
                target.Defense,
                context.RandomService,
                damageMultiplier);

            if (damage <= Mathf.Epsilon)
            {
                return;
            }

            BattleDamageSystem.ApplyResolvedDamage(
                context,
                battleCallbacks,
                proxy.Owner,
                target,
                damage,
                sourceKind,
                sourceSkill,
                sourceBasicAttackVariantKey: string.Empty,
                sourceProxy: proxy);
        }

        private static void TryFirePeriodicProxyAttack(
            BattleContext context,
            RuntimeDeployableProxy proxy,
            IBattleSimulationCallbacks battleCallbacks)
        {
            if (context == null
                || proxy == null
                || proxy.IsExpired
                || proxy.Owner == null
                || battleCallbacks == null
                || !proxy.TryConsumeReadyAttack())
            {
                return;
            }

            if (!BattleBasicAttackSystem.TryResolveProxyAttack(context, proxy, out var target, out var resolvedAttack))
            {
                return;
            }

            BattleBasicAttackSystem.FireProxyAttack(context, proxy, target, resolvedAttack, battleCallbacks);
        }

        private static void TryFirePeriodicProxyEffectPulse(BattleContext context, RuntimeDeployableProxy proxy)
        {
            if (context == null
                || proxy == null
                || proxy.IsExpired
                || proxy.Owner == null
                || proxy.SourceEffect == null
                || !proxy.TryConsumeReadyEffectPulse())
            {
                return;
            }

            var targets = CollectPulseTargets(context, proxy);
            context.EventBus?.Publish(new DeployableProxyPulseEvent(proxy, targets.Count));
            if (targets.Count <= 0)
            {
                return;
            }

            BattleForcedMovementUtility.ApplyForcedMovementToTargets(
                context,
                proxy.Owner,
                proxy.CurrentPosition,
                proxy.SourceSkill,
                proxy.SourceEffect,
                targets);
        }

        private static bool TryFireProximityExplosion(
            BattleContext context,
            RuntimeDeployableProxy proxy,
            IBattleSimulationCallbacks battleCallbacks)
        {
            if (context == null
                || proxy == null
                || proxy.IsExpired
                || proxy.Owner == null
                || proxy.SourceEffect == null
                || proxy.TriggerMode != DeployableProxyTriggerMode.ProximityExplosion
                || battleCallbacks == null
                || !HasProximityExplosionTriggerTarget(context, proxy))
            {
                return false;
            }

            var targets = CollectProximityExplosionTargets(context, proxy);
            context.EventBus?.Publish(new DeployableProxyPulseEvent(proxy, targets.Count));
            ResolveProxyExplosion(context, proxy, targets, battleCallbacks);
            proxy.ExpireImmediately();
            return true;
        }

        private static bool HasProximityExplosionTriggerTarget(BattleContext context, RuntimeDeployableProxy proxy)
        {
            if (context?.Heroes == null || proxy?.Owner == null)
            {
                return false;
            }

            var triggerRadius = proxy.TriggerRadius;
            if (triggerRadius <= Mathf.Epsilon)
            {
                return false;
            }

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (!IsValidProximityExplosionTarget(proxy.Owner, candidate))
                {
                    continue;
                }

                if (Vector3.Distance(proxy.CurrentPosition, candidate.CurrentPosition) <= triggerRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<RuntimeHero> CollectProximityExplosionTargets(BattleContext context, RuntimeDeployableProxy proxy)
        {
            var results = new List<RuntimeHero>();
            if (context?.Heroes == null || proxy?.Owner == null)
            {
                return results;
            }

            var effectRadius = proxy.EffectRadius;
            if (effectRadius <= Mathf.Epsilon)
            {
                return results;
            }

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (!IsValidProximityExplosionTarget(proxy.Owner, candidate))
                {
                    continue;
                }

                if (Vector3.Distance(proxy.CurrentPosition, candidate.CurrentPosition) <= effectRadius)
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static bool IsValidProximityExplosionTarget(RuntimeHero owner, RuntimeHero target)
        {
            return owner != null
                && target != null
                && !target.IsDead
                && target.Side != owner.Side;
        }

        private static void ResolveProxyExplosion(
            BattleContext context,
            RuntimeDeployableProxy proxy,
            IReadOnlyList<RuntimeHero> targets,
            IBattleSimulationCallbacks battleCallbacks)
        {
            if (context == null
                || proxy?.Owner == null
                || targets == null
                || targets.Count <= 0
                || battleCallbacks == null)
            {
                return;
            }

            var damageMultiplier = proxy.StrikePowerMultiplier;
            if (damageMultiplier <= Mathf.Epsilon)
            {
                return;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!IsValidProximityExplosionTarget(proxy.Owner, target))
                {
                    continue;
                }

                var damage = DamageResolver.ResolveDamage(
                    proxy.Owner.AttackPower,
                    proxy.Owner.CriticalChance,
                    proxy.Owner.CriticalDamageMultiplier,
                    target.Defense,
                    context.RandomService,
                    damageMultiplier);
                if (damage <= Mathf.Epsilon)
                {
                    continue;
                }

                BattleDamageSystem.ApplyResolvedDamage(
                    context,
                    battleCallbacks,
                    proxy.Owner,
                    target,
                    damage,
                    DamageSourceKind.Skill,
                    proxy.SourceSkill,
                    sourceBasicAttackVariantKey: string.Empty,
                    sourceProxy: proxy);
            }
        }

        private static List<RuntimeHero> CollectPulseTargets(BattleContext context, RuntimeDeployableProxy proxy)
        {
            var results = new List<RuntimeHero>();
            if (context?.Heroes == null || proxy?.Owner == null)
            {
                return results;
            }

            var radius = proxy.StrikeRadius;
            if (radius <= Mathf.Epsilon)
            {
                return results;
            }

            var targetType = proxy.SourceEffect != null
                ? proxy.SourceEffect.persistentAreaTargetType
                : PersistentAreaTargetType.Enemies;
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (!IsValidPulseTarget(proxy.Owner, candidate, targetType))
                {
                    continue;
                }

                if (Vector3.Distance(proxy.CurrentPosition, candidate.CurrentPosition) <= radius)
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static bool IsValidPulseTarget(RuntimeHero owner, RuntimeHero target, PersistentAreaTargetType targetType)
        {
            if (owner == null || target == null || target.IsDead)
            {
                return false;
            }

            if (target != owner && !target.CanBeDirectTargeted)
            {
                return false;
            }

            return targetType switch
            {
                PersistentAreaTargetType.Allies => target.Side == owner.Side,
                PersistentAreaTargetType.Both => true,
                _ => target.Side != owner.Side,
            };
        }
    }
}
