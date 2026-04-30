using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleDamageTriggeredStatusSystem
    {
        public static void TryProcessDamage(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeHero target,
            DamageSourceKind sourceKind,
            SkillData sourceSkill)
        {
            if (context == null
                || attacker == null
                || target == null
                || attacker.IsDead
                || target.IsDead
                || attacker.Side == target.Side
                || !TryGetCounterSkill(attacker, sourceSkill, out var counterSkill, out var counterData)
                || !ShouldCountDamageSource(counterData, sourceKind))
            {
                return;
            }

            var counterStatus = counterData.countedStatus;
            if (counterStatus == null || counterStatus.effectType == StatusEffectType.None)
            {
                return;
            }

            var effectSourceSkill = sourceSkill ?? counterSkill;

            var previousStackCount = StatusEffectSystem.GetStatusStackCount(
                target,
                counterStatus.effectType,
                counterStatus.statusThemeKey,
                attacker,
                counterStatus.stackGroupKey);
            if (!target.ApplyStatusEffect(counterStatus, attacker, effectSourceSkill, attacker, out _))
            {
                return;
            }

            BattleStatsSystem.RecordStatusContribution(context, attacker, target, counterStatus);

            var currentStackCount = StatusEffectSystem.GetStatusStackCount(
                target,
                counterStatus.effectType,
                counterStatus.statusThemeKey,
                attacker,
                counterStatus.stackGroupKey);
            context.EventBus?.Publish(new StatusCounterChangedEvent(
                attacker,
                target,
                counterStatus.effectType,
                counterStatus.statusThemeKey,
                effectSourceSkill,
                sourceKind,
                previousStackCount,
                currentStackCount,
                Mathf.Max(1, counterStatus.maxStacks)));

            var threshold = Mathf.Max(1, counterData.triggerThreshold);
            if (previousStackCount >= threshold || currentStackCount < threshold)
            {
                return;
            }

            ApplyThresholdStatuses(context, attacker, target, effectSourceSkill, counterData.triggerStatusEffects);

            if (counterData.clearCountedStatusesOnTrigger)
            {
                StatusEffectSystem.RemoveStatuses(
                    target,
                    counterStatus.effectType,
                    counterStatus.statusThemeKey,
                    status => PublishStatusRemovedEvent(context, target, status),
                    attacker,
                    counterStatus.stackGroupKey);
            }

            context.EventBus?.Publish(new StatusCounterThresholdTriggeredEvent(
                attacker,
                target,
                counterStatus.effectType,
                counterStatus.statusThemeKey,
                effectSourceSkill,
                sourceKind,
                threshold,
                currentStackCount));
        }

        private static bool TryGetCounterSkill(
            RuntimeHero attacker,
            SkillData sourceSkill,
            out SkillData counterSkill,
            out DamageTriggeredStatusCounterData counterData)
        {
            counterSkill = null;
            counterData = null;
            if (attacker?.Definition == null)
            {
                return false;
            }

            if (HasValidCounter(sourceSkill))
            {
                counterSkill = sourceSkill;
                counterData = sourceSkill.damageTriggeredStatusCounter;
                return true;
            }

            var activeSkill = attacker.Definition.activeSkill;
            if (activeSkill != sourceSkill && HasValidCounter(activeSkill))
            {
                counterSkill = activeSkill;
                counterData = activeSkill.damageTriggeredStatusCounter;
                return true;
            }

            var ultimateSkill = attacker.Definition.ultimateSkill;
            if (ultimateSkill != sourceSkill && ultimateSkill != activeSkill && HasValidCounter(ultimateSkill))
            {
                counterSkill = ultimateSkill;
                counterData = ultimateSkill.damageTriggeredStatusCounter;
                return true;
            }

            return false;
        }

        private static bool HasValidCounter(SkillData skill)
        {
            return skill != null
                && skill.damageTriggeredStatusCounter != null
                && skill.damageTriggeredStatusCounter.enabled
                && skill.damageTriggeredStatusCounter.countedStatus != null
                && skill.damageTriggeredStatusCounter.countedStatus.effectType != StatusEffectType.None;
        }

        private static bool ShouldCountDamageSource(DamageTriggeredStatusCounterData counterData, DamageSourceKind sourceKind)
        {
            if (counterData == null)
            {
                return false;
            }

            return sourceKind switch
            {
                DamageSourceKind.BasicAttack => counterData.countBasicAttackDamage,
                DamageSourceKind.Skill => counterData.countSkillDamage,
                DamageSourceKind.ChanneledPathSkillTick => counterData.countSkillDamage,
                DamageSourceKind.SkillAreaPulse => counterData.countSkillAreaPulseDamage,
                DamageSourceKind.StatusEffect => counterData.countStatusEffectDamage,
                DamageSourceKind.CounterTrigger => counterData.countCounterTriggerDamage,
                _ => false,
            };
        }

        private static void ApplyThresholdStatuses(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeHero target,
            SkillData sourceSkill,
            IReadOnlyList<StatusEffectData> statuses)
        {
            if (context == null || attacker == null || target == null || target.IsDead || statuses == null)
            {
                return;
            }

            for (var i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status == null)
                {
                    continue;
                }

                var previousShield = status.effectType == StatusEffectType.Shield
                    ? StatusEffectSystem.GetTotalShield(target)
                    : 0f;
                if (!target.ApplyStatusEffect(status, attacker, sourceSkill, attacker, out var appliedStatus))
                {
                    continue;
                }

                var appliedSource = appliedStatus?.Source ?? attacker;
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
                    appliedStatus?.Magnitude ?? status.magnitude,
                    sourceSkill,
                    appliedStatus?.AppliedBy ?? attacker));
            }
        }

        private static void PublishStatusRemovedEvent(BattleContext context, RuntimeHero target, RuntimeStatusEffect status)
        {
            if (context?.EventBus == null || target == null || status == null)
            {
                return;
            }

            context.EventBus.Publish(new StatusRemovedEvent(
                status.Source,
                target,
                status.EffectType,
                status.SourceSkill,
                status.AppliedBy));
        }
    }
}
