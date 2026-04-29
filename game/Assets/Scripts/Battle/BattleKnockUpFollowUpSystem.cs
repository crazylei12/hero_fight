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
            SkillData sourceSkill)
        {
            Source = source;
            Target = target;
            AppliedBy = appliedBy;
            EffectType = effectType;
            SourceSkill = sourceSkill;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero Target { get; }

        public RuntimeHero AppliedBy { get; }

        public StatusEffectType EffectType { get; }

        public SkillData SourceSkill { get; }
    }

    public static class BattleKnockUpFollowUpSystem
    {
        private static readonly List<RuntimeKnockUpFollowUpTrigger> reusableTriggers = new List<RuntimeKnockUpFollowUpTrigger>();

        public static void Capture(BattleContext context, IBattleEvent battleEvent)
        {
            if (context?.KnockUpFollowUpTriggers == null
                || battleEvent is not StatusAppliedEvent statusApplied
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

            var landingAnchor = SelectRandomTarget(context, targets);
            context.EventBus.Publish(new KnockUpFollowUpTriggeredEvent(
                follower,
                skill,
                firstTrigger?.Source,
                firstTrigger?.SourceSkill,
                targets.Count,
                landingAnchor,
                followUp.damagePowerMultiplier));
            context.EventBus.Publish(new SkillCastEvent(follower, skill, landingAnchor, targets.Count));

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || target.IsDead || !target.CanReceiveDamage)
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
            }

            landingAnchor = ResolveLandingAnchorAfterDamage(context, follower, targets, landingAnchor);
            if (landingAnchor != null)
            {
                ApplyLanding(context, follower, skill, landingAnchor, followUp);
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
                || trigger.EffectType != followUp.triggerStatusEffectType
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
            RuntimeHero landingAnchor,
            KnockUpFollowUpData followUp)
        {
            if (context == null || follower == null || landingAnchor == null || followUp == null)
            {
                return;
            }

            var startPosition = follower.CurrentPosition;
            var destination = ResolveLandingPosition(context, follower, landingAnchor, followUp.landingDistance);
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
