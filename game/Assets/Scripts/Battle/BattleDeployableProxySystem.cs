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
