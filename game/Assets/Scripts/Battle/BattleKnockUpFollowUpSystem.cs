using System.Collections.Generic;
using Fight.Core;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public sealed class RuntimeKnockUpFollowUpTrigger
    {
        public RuntimeKnockUpFollowUpTrigger(
            RuntimeHero source,
            RuntimeHero target,
            RuntimeHero appliedBy,
            StatusEffectType effectType,
            SkillData sourceSkill,
            bool isKnockback = false)
        {
            Source = source;
            Target = target;
            AppliedBy = appliedBy;
            EffectType = effectType;
            SourceSkill = sourceSkill;
            IsKnockback = isKnockback;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero Target { get; }

        public RuntimeHero AppliedBy { get; }

        public StatusEffectType EffectType { get; }

        public SkillData SourceSkill { get; }

        public bool IsKnockback { get; }

        public string TriggerKind => IsKnockback ? "Knockback" : EffectType.ToString();
    }

    public static class BattleKnockUpFollowUpSystem
    {
        private static readonly List<RuntimeKnockUpFollowUpTrigger> reusableTriggers = new List<RuntimeKnockUpFollowUpTrigger>();

        public static void Capture(BattleContext context, IBattleEvent battleEvent)
        {
            if (context?.KnockUpFollowUpTriggers == null || battleEvent == null)
            {
                return;
            }

            if (battleEvent is StatusAppliedEvent statusApplied)
            {
                CaptureKnockUp(context, statusApplied);
                return;
            }

            if (battleEvent is ForcedMovementAppliedEvent forcedMovement)
            {
                CaptureKnockback(context, forcedMovement);
            }
        }

        private static void CaptureKnockUp(BattleContext context, StatusAppliedEvent statusApplied)
        {
            if (statusApplied == null
                || statusApplied.EffectType != StatusEffectType.KnockUp
                || statusApplied.Source == null
                || statusApplied.Target == null
                || statusApplied.Target.IsDead
                || statusApplied.Source.Side == statusApplied.Target.Side)
            {
                return;
            }

            context.KnockUpFollowUpTriggers.Add(new RuntimeKnockUpFollowUpTrigger(
                statusApplied.Source,
                statusApplied.Target,
                statusApplied.AppliedBy,
                statusApplied.EffectType,
                statusApplied.SourceSkill));
        }

        private static void CaptureKnockback(BattleContext context, ForcedMovementAppliedEvent forcedMovement)
        {
            if (forcedMovement == null
                || !forcedMovement.CountsAsKnockback
                || forcedMovement.Source == null
                || forcedMovement.Target == null
                || forcedMovement.Target.IsDead
                || forcedMovement.Source.Side == forcedMovement.Target.Side)
            {
                return;
            }

            context.KnockUpFollowUpTriggers.Add(new RuntimeKnockUpFollowUpTrigger(
                forcedMovement.Source,
                forcedMovement.Target,
                forcedMovement.Source,
                StatusEffectType.None,
                forcedMovement.SourceSkill,
                isKnockback: true));
        }

        public static void Flush(BattleContext context, IBattleSimulationCallbacks battleCallbacks)
        {
            if (context?.KnockUpFollowUpTriggers == null
                || context.KnockUpFollowUpTriggers.Count == 0
                || context.Heroes == null
                || battleCallbacks == null)
            {
                return;
            }

            reusableTriggers.Clear();
            reusableTriggers.AddRange(context.KnockUpFollowUpTriggers);
            context.KnockUpFollowUpTriggers.Clear();

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                ResolveFollower(context, battleCallbacks, context.Heroes[i], reusableTriggers);
            }

            reusableTriggers.Clear();
        }

        private static void ResolveFollower(
            BattleContext context,
            IBattleSimulationCallbacks battleCallbacks,
            RuntimeHero follower,
            IReadOnlyList<RuntimeKnockUpFollowUpTrigger> triggers)
        {
            var skill = follower?.Definition?.ultimateSkill;
            var followUp = skill?.knockUpFollowUp;
            if (follower == null
                || follower.IsDead
                || skill == null
                || skill.slotType != SkillSlotType.Ultimate
                || skill.activationMode != SkillActivationMode.Passive
                || followUp == null
                || !followUp.enabled)
            {
                return;
            }

            var targets = new List<RuntimeHero>();
            RuntimeKnockUpFollowUpTrigger firstTrigger = null;
            for (var i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                if (!IsValidTriggerForFollower(follower, followUp, trigger))
                {
                    continue;
                }

                if (targets.Contains(trigger.Target))
                {
                    continue;
                }

                targets.Add(trigger.Target);
                firstTrigger ??= trigger;
            }

            if (targets.Count == 0)
            {
                return;
            }

            var fallbackLandingPosition = Stage01ArenaSpec.ClampPosition(follower.CurrentPosition);
            fallbackLandingPosition.y = 0f;
            var landingAnchor = SelectRandomTarget(context, targets);
            var damagedTargetCount = 0;

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!IsValidDamageTarget(follower, target))
                {
                    continue;
                }

                var damage = DamageResolver.ResolveDamage(
                    follower.AttackPower,
                    follower.CriticalChance,
                    follower.CriticalDamageMultiplier,
                    target.Defense,
                    context.RandomService,
                    followUp.damagePowerMultiplier);
                BattleDamageSystem.ApplyResolvedDamage(
                    context,
                    battleCallbacks,
                    follower,
                    target,
                    damage,
                    DamageSourceKind.Skill,
                    skill);
                damagedTargetCount++;
            }

            landingAnchor = ResolveLandingAnchorAfterDamage(context, follower, targets, landingAnchor);
            var usedFallbackLanding = landingAnchor == null;
            var landingDestination = usedFallbackLanding
                ? fallbackLandingPosition
                : ResolveLandingPosition(context, follower, landingAnchor, followUp.landingDistance);
            context.EventBus.Publish(new KnockUpFollowUpTriggeredEvent(
                follower,
                skill,
                firstTrigger?.Source,
                firstTrigger?.SourceSkill,
                firstTrigger?.TriggerKind,
                damagedTargetCount,
                landingAnchor,
                landingDestination,
                usedFallbackLanding,
                followUp.damagePowerMultiplier));
            context.EventBus.Publish(new SkillCastEvent(follower, skill, landingAnchor, damagedTargetCount));

            if (!follower.IsDead)
            {
                ApplyLanding(context, follower, skill, landingDestination, followUp);
            }
        }

        private static bool IsValidTriggerForFollower(
            RuntimeHero follower,
            KnockUpFollowUpData followUp,
            RuntimeKnockUpFollowUpTrigger trigger)
        {
            if (follower == null
                || followUp == null
                || trigger?.Source == null
                || trigger.Target == null
                || !IsEnabledTrigger(followUp, trigger)
                || trigger.Source == follower
                || trigger.Source.Side != follower.Side
                || trigger.Target.Side == follower.Side
                || trigger.Target.IsDead
                || !trigger.Target.CanBeDirectTargeted)
            {
                return false;
            }

            return true;
        }

        private static bool IsValidDamageTarget(RuntimeHero follower, RuntimeHero target)
        {
            return IsValidLandingAnchor(follower, target)
                && target.CanReceiveDamage;
        }

        private static bool IsEnabledTrigger(KnockUpFollowUpData followUp, RuntimeKnockUpFollowUpTrigger trigger)
        {
            if (followUp == null || trigger == null)
            {
                return false;
            }

            if (trigger.IsKnockback)
            {
                return followUp.triggerStatusEffectType == StatusEffectType.KnockUp;
            }

            return trigger.EffectType == followUp.triggerStatusEffectType;
        }

        private static RuntimeHero ResolveLandingAnchorAfterDamage(
            BattleContext context,
            RuntimeHero follower,
            IReadOnlyList<RuntimeHero> targets,
            RuntimeHero preferredAnchor)
        {
            if (IsValidLandingAnchor(follower, preferredAnchor))
            {
                return preferredAnchor;
            }

            var remainingTargets = new List<RuntimeHero>();
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (IsValidLandingAnchor(follower, target))
                {
                    remainingTargets.Add(target);
                }
            }

            return SelectRandomTarget(context, remainingTargets);
        }

        private static bool IsValidLandingAnchor(RuntimeHero follower, RuntimeHero target)
        {
            return follower != null
                && target != null
                && !target.IsDead
                && target.Side != follower.Side
                && target.CanBeDirectTargeted;
        }

        private static RuntimeHero SelectRandomTarget(BattleContext context, IReadOnlyList<RuntimeHero> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                return null;
            }

            var index = context?.RandomService != null
                ? context.RandomService.Range(0, targets.Count)
                : 0;
            return targets[Mathf.Clamp(index, 0, targets.Count - 1)];
        }

        private static void ApplyLanding(
            BattleContext context,
            RuntimeHero follower,
            SkillData skill,
            Vector3 destination,
            KnockUpFollowUpData followUp)
        {
            if (context == null || follower == null || followUp == null)
            {
                return;
            }

            var startPosition = follower.CurrentPosition;
            destination = Stage01ArenaSpec.ClampPosition(destination);
            destination.y = 0f;
            var durationSeconds = Mathf.Max(0f, followUp.landingDurationSeconds);
            var peakHeight = Mathf.Max(0f, followUp.landingPeakHeight);
            follower.StartForcedMovement(destination, durationSeconds, peakHeight);
            context.EventBus.Publish(new ForcedMovementAppliedEvent(
                follower,
                follower,
                startPosition,
                destination,
                durationSeconds,
                peakHeight,
                skill));
        }

        private static Vector3 ResolveLandingPosition(
            BattleContext context,
            RuntimeHero follower,
            RuntimeHero landingAnchor,
            float landingDistance)
        {
            var distance = Mathf.Max(0f, landingDistance);
            var direction = follower.CurrentPosition - landingAnchor.CurrentPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                var angle = context?.RandomService != null
                    ? context.RandomService.Range(0f, 360f)
                    : 0f;
                var radians = angle * Mathf.Deg2Rad;
                direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
            }

            var destination = landingAnchor.CurrentPosition + direction.normalized * distance;
            destination.y = 0f;
            return Stage01ArenaSpec.ClampPosition(destination);
        }
    }
}
