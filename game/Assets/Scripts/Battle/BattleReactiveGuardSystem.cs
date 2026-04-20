using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleReactiveGuardSystem
    {
        public static void RegisterReactiveGuard(
            BattleContext context,
            RuntimeHero caster,
            RuntimeHero protectedHero,
            SkillData sourceSkill,
            ReactiveGuardData guardData)
        {
            if (context?.ReactiveGuards == null
                || caster == null
                || protectedHero == null
                || protectedHero.IsDead
                || guardData == null
                || !guardData.enabled
                || guardData.durationSeconds <= Mathf.Epsilon)
            {
                return;
            }

            RemoveMatchingGuard(context.ReactiveGuards, caster, protectedHero, sourceSkill);
            var guard = new RuntimeReactiveGuard(caster, protectedHero, sourceSkill, guardData);
            context.ReactiveGuards.Add(guard);
            TryTriggerGuard(context, guard);
        }

        public static void Tick(BattleContext context, float deltaTime)
        {
            if (context?.ReactiveGuards == null)
            {
                return;
            }

            for (var i = context.ReactiveGuards.Count - 1; i >= 0; i--)
            {
                var guard = context.ReactiveGuards[i];
                if (!IsGuardStillValid(guard))
                {
                    context.ReactiveGuards.RemoveAt(i);
                    continue;
                }

                guard.Tick(deltaTime);
                if (guard.IsExpired)
                {
                    context.ReactiveGuards.RemoveAt(i);
                    continue;
                }

                TryTriggerGuard(context, guard);
                if (guard.IsExpired)
                {
                    context.ReactiveGuards.RemoveAt(i);
                }
            }
        }

        private static bool IsGuardStillValid(RuntimeReactiveGuard guard)
        {
            return guard != null
                && guard.Caster != null
                && !guard.Caster.IsDead
                && guard.ProtectedHero != null
                && !guard.ProtectedHero.IsDead
                && guard.Caster.Side == guard.ProtectedHero.Side;
        }

        private static void RemoveMatchingGuard(
            List<RuntimeReactiveGuard> guards,
            RuntimeHero caster,
            RuntimeHero protectedHero,
            SkillData sourceSkill)
        {
            if (guards == null)
            {
                return;
            }

            for (var i = guards.Count - 1; i >= 0; i--)
            {
                var guard = guards[i];
                if (guard == null)
                {
                    guards.RemoveAt(i);
                    continue;
                }

                if (guard.Caster == caster
                    && guard.ProtectedHero == protectedHero
                    && guard.SourceSkill == sourceSkill)
                {
                    guards.RemoveAt(i);
                }
            }
        }

        private static void TryTriggerGuard(BattleContext context, RuntimeReactiveGuard guard)
        {
            if (!IsGuardStillValid(guard) || guard.IsExpired)
            {
                guard?.ExpireImmediately();
                return;
            }

            var triggerTargets = CollectEnemyTargets(context, guard.ProtectedHero, Mathf.Max(0f, guard.TriggerRadius));
            if (triggerTargets.Count <= 0)
            {
                return;
            }

            var effectRadius = guard.EffectRadius > Mathf.Epsilon ? guard.EffectRadius : guard.TriggerRadius;
            var affectedTargets = CollectEnemyTargets(context, guard.ProtectedHero, Mathf.Max(0f, effectRadius));
            var successfulKnockUpTargetCount = ApplyTriggerStatuses(context, guard, affectedTargets);
            ApplyTriggerForcedMovement(context, guard, affectedTargets);
            ApplyTriggerHealing(context, guard, successfulKnockUpTargetCount);
            context.EventBus?.Publish(new ReactiveGuardTriggeredEvent(
                guard.Caster,
                guard.ProtectedHero,
                guard.SourceSkill,
                affectedTargets.Count));
            guard.ConsumeTrigger();
        }

        private static List<RuntimeHero> CollectEnemyTargets(BattleContext context, RuntimeHero protectedHero, float radius)
        {
            var results = new List<RuntimeHero>();
            if (context?.Heroes == null || protectedHero == null)
            {
                return results;
            }

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate == null || candidate.IsDead || candidate.Side == protectedHero.Side)
                {
                    continue;
                }

                if (Vector3.Distance(candidate.CurrentPosition, protectedHero.CurrentPosition) <= radius)
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static int ApplyTriggerStatuses(BattleContext context, RuntimeReactiveGuard guard, List<RuntimeHero> affectedTargets)
        {
            if (guard?.OnTriggerStatusEffects == null || affectedTargets == null)
            {
                return 0;
            }

            HashSet<string> successfulKnockUpTargetIds = null;
            for (var targetIndex = 0; targetIndex < affectedTargets.Count; targetIndex++)
            {
                var target = affectedTargets[targetIndex];
                for (var statusIndex = 0; statusIndex < guard.OnTriggerStatusEffects.Count; statusIndex++)
                {
                    var status = guard.OnTriggerStatusEffects[statusIndex];
                    if (status == null)
                    {
                        continue;
                    }

                    var previousShield = status.effectType == StatusEffectType.Shield
                        ? StatusEffectSystem.GetTotalShield(target)
                        : 0f;
                    if (!target.ApplyStatusEffect(status, guard.Caster, guard.SourceSkill, guard.ProtectedHero, out var appliedStatus))
                    {
                        continue;
                    }

                    var appliedSource = appliedStatus?.Source ?? guard.Caster;
                    BattleStatsSystem.RecordStatusContribution(context, appliedSource, target, status);
                    if (status.effectType == StatusEffectType.Shield)
                    {
                        var shieldDelta = Mathf.Max(0f, StatusEffectSystem.GetTotalShield(target) - previousShield);
                        BattleStatsSystem.RecordShieldContribution(context, appliedSource, target, shieldDelta);
                    }

                    context.EventBus?.Publish(new StatusAppliedEvent(
                        appliedSource,
                        target,
                        status.effectType,
                        status.durationSeconds,
                        status.magnitude,
                        guard.SourceSkill,
                        appliedStatus?.AppliedBy ?? guard.ProtectedHero));

                    if (status.effectType == StatusEffectType.KnockUp && !string.IsNullOrEmpty(target.RuntimeId))
                    {
                        successfulKnockUpTargetIds ??= new HashSet<string>();
                        successfulKnockUpTargetIds.Add(target.RuntimeId);
                    }
                }
            }

            return successfulKnockUpTargetIds?.Count ?? 0;
        }

        private static void ApplyTriggerForcedMovement(BattleContext context, RuntimeReactiveGuard guard, List<RuntimeHero> affectedTargets)
        {
            if (guard == null
                || affectedTargets == null
                || (guard.ForcedMovementDistance <= Mathf.Epsilon && guard.ForcedMovementDurationSeconds <= Mathf.Epsilon))
            {
                return;
            }

            var sourcePosition = guard.ProtectedHero.CurrentPosition;
            for (var i = 0; i < affectedTargets.Count; i++)
            {
                var target = affectedTargets[i];
                if (target == null || target.IsDead)
                {
                    continue;
                }

                var offset = target.CurrentPosition - sourcePosition;
                offset.y = 0f;
                var direction = offset.sqrMagnitude > Mathf.Epsilon
                    ? offset.normalized
                    : (target.Side == TeamSide.Blue ? Vector3.left : Vector3.right);
                var destination = Stage01ArenaSpec.ClampPosition(target.CurrentPosition + direction * guard.ForcedMovementDistance);
                destination.y = 0f;
                var startPosition = target.CurrentPosition;
                target.StartForcedMovement(destination, guard.ForcedMovementDurationSeconds, guard.ForcedMovementPeakHeight);
                BattleStatsSystem.RecordForcedMovementContribution(context, guard.Caster, target);
                context.EventBus?.Publish(new ForcedMovementAppliedEvent(
                    guard.Caster,
                    target,
                    startPosition,
                    destination,
                    guard.ForcedMovementDurationSeconds,
                    guard.ForcedMovementPeakHeight,
                    guard.SourceSkill));
            }
        }

        private static void ApplyTriggerHealing(BattleContext context, RuntimeReactiveGuard guard, int successfulKnockUpTargetCount)
        {
            if (guard == null
                || guard.ProtectedHero == null
                || successfulKnockUpTargetCount <= 0
                || guard.HealProtectedHeroPerSuccessfulKnockUp <= Mathf.Epsilon)
            {
                return;
            }

            var actualHeal = guard.ProtectedHero.ApplyHealing(successfulKnockUpTargetCount * guard.HealProtectedHeroPerSuccessfulKnockUp);
            if (actualHeal <= 0f)
            {
                return;
            }

            BattleStatsSystem.RecordHealingContribution(context, guard.Caster, guard.ProtectedHero, actualHeal);
            context.EventBus?.Publish(new HealAppliedEvent(
                guard.Caster,
                guard.ProtectedHero,
                actualHeal,
                guard.SourceSkill,
                guard.ProtectedHero.CurrentHealth));
        }
    }
}
