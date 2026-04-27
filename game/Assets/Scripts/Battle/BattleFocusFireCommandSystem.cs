using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleFocusFireCommandSystem
    {
        private const string DefaultFocusFireStackGroupKey = "focus_fire_command";

        public static void Register(
            BattleContext context,
            RuntimeHero source,
            SkillData skill,
            RuntimeHero initialTarget,
            SkillEffectData effect)
        {
            if (context?.FocusFireCommands == null
                || source == null
                || skill == null
                || effect == null)
            {
                return;
            }

            RemoveExistingTeamCommands(context, source.Side, FocusFireCommandTargetChangeReason.Replaced);

            var durationSeconds = ResolveCommandDuration(effect);
            if (durationSeconds <= Mathf.Epsilon)
            {
                return;
            }

            var command = new RuntimeFocusFireCommand(
                source,
                skill,
                source.Side,
                source.CurrentPosition,
                skill.castRange,
                durationSeconds,
                effect.statusEffects);

            context.FocusFireCommands.Add(command);
            var target = IsValidFocusTarget(command.SourceSide, initialTarget)
                ? initialTarget
                : SelectNextTarget(context, command);
            Retarget(context, command, target, FocusFireCommandTargetChangeReason.Started);
        }

        public static void Tick(BattleContext context, float deltaTime)
        {
            if (context?.FocusFireCommands == null || context.FocusFireCommands.Count == 0)
            {
                return;
            }

            for (var i = context.FocusFireCommands.Count - 1; i >= 0; i--)
            {
                var command = context.FocusFireCommands[i];
                if (command == null)
                {
                    context.FocusFireCommands.RemoveAt(i);
                    continue;
                }

                command.Tick(deltaTime);
                if (command.IsExpired)
                {
                    RemoveCurrentStatuses(context, command);
                    PublishTargetChanged(context, command, command.CurrentTarget, null, FocusFireCommandTargetChangeReason.Expired);
                    context.FocusFireCommands.RemoveAt(i);
                    continue;
                }

                if (IsValidFocusTarget(command.SourceSide, command.CurrentTarget))
                {
                    continue;
                }

                var nextTarget = SelectNextTarget(context, command);
                Retarget(context, command, nextTarget, FocusFireCommandTargetChangeReason.TargetInvalid);
            }
        }

        public static bool TryGetMarkedTarget(
            BattleContext context,
            RuntimeHero attacker,
            Vector3 sourcePosition,
            float selectionRange,
            out RuntimeHero target)
        {
            target = null;
            if (context?.FocusFireCommands == null || attacker == null)
            {
                return false;
            }

            for (var i = 0; i < context.FocusFireCommands.Count; i++)
            {
                var command = context.FocusFireCommands[i];
                if (command == null
                    || command.SourceSide != attacker.Side
                    || !IsValidFocusTarget(attacker.Side, command.CurrentTarget)
                    || !IsWithinRange(sourcePosition, command.CurrentTarget, selectionRange))
                {
                    continue;
                }

                target = command.CurrentTarget;
                return true;
            }

            return false;
        }

        private static void Retarget(
            BattleContext context,
            RuntimeFocusFireCommand command,
            RuntimeHero nextTarget,
            FocusFireCommandTargetChangeReason reason)
        {
            if (context == null || command == null)
            {
                return;
            }

            var previousTarget = command.CurrentTarget;
            if (previousTarget == nextTarget && IsValidFocusTarget(command.SourceSide, nextTarget))
            {
                RefreshCurrentStatuses(context, command);
                return;
            }

            RemoveCurrentStatuses(context, command);
            command.SetCurrentTarget(IsValidFocusTarget(command.SourceSide, nextTarget) ? nextTarget : null);
            ApplyCurrentStatuses(context, command);
            PublishTargetChanged(context, command, previousTarget, command.CurrentTarget, reason);
        }

        private static void RefreshCurrentStatuses(BattleContext context, RuntimeFocusFireCommand command)
        {
            RemoveCurrentStatuses(context, command);
            ApplyCurrentStatuses(context, command);
        }

        private static void ApplyCurrentStatuses(BattleContext context, RuntimeFocusFireCommand command)
        {
            var target = command?.CurrentTarget;
            if (context?.EventBus == null
                || target == null
                || target.IsDead
                || command.StatusEffects == null)
            {
                return;
            }

            for (var i = 0; i < command.StatusEffects.Count; i++)
            {
                var status = CreateRuntimeStatusData(command.StatusEffects[i], command.RemainingDurationSeconds);
                if (status == null)
                {
                    continue;
                }

                var previousShield = status.effectType == StatusEffectType.Shield
                    ? StatusEffectSystem.GetTotalShield(target)
                    : 0f;
                if (!target.ApplyStatusEffect(status, command.Source, command.Skill, command.Source, out var appliedStatus))
                {
                    continue;
                }

                var appliedSource = appliedStatus?.Source ?? command.Source;
                BattleStatsSystem.RecordStatusContribution(context, appliedSource, target, status);
                if (status.effectType == StatusEffectType.Shield)
                {
                    var shieldDelta = Mathf.Max(0f, StatusEffectSystem.GetTotalShield(target) - previousShield);
                    BattleStatsSystem.RecordShieldContribution(context, appliedSource, target, shieldDelta);
                }

                context.EventBus.Publish(new StatusAppliedEvent(
                    appliedSource,
                    target,
                    status.effectType,
                    status.durationSeconds,
                    appliedStatus?.Magnitude ?? status.magnitude,
                    command.Skill,
                    appliedStatus?.AppliedBy ?? command.Source));
            }
        }

        private static void RemoveCurrentStatuses(BattleContext context, RuntimeFocusFireCommand command)
        {
            var target = command?.CurrentTarget;
            if (context?.EventBus == null
                || target == null
                || command.StatusEffects == null)
            {
                return;
            }

            for (var i = 0; i < command.StatusEffects.Count; i++)
            {
                var status = command.StatusEffects[i];
                if (status == null || status.effectType == StatusEffectType.None)
                {
                    continue;
                }

                var stackGroupKey = GetFocusStackGroupKey(status);
                StatusEffectSystem.RemoveStatuses(
                    target,
                    status.effectType,
                    onRemovedStatus: removedStatus => PublishStatusRemovedEvent(context, target, removedStatus),
                    source: command.Source,
                    stackGroupKey: stackGroupKey);
            }
        }

        private static RuntimeHero SelectNextTarget(BattleContext context, RuntimeFocusFireCommand command)
        {
            if (context?.Heroes == null || command == null)
            {
                return null;
            }

            RuntimeHero best = null;
            var highestDamageDealt = float.MinValue;
            var lowestCurrentHealth = float.MaxValue;
            var nearestDistance = float.MaxValue;

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (!IsValidFocusTarget(command.SourceSide, candidate)
                    || !IsWithinRange(command.OriginPosition, candidate, command.SelectionRange))
                {
                    continue;
                }

                var distance = Vector3.Distance(command.OriginPosition, candidate.CurrentPosition);
                if (!IsBetterFocusTarget(
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

        private static bool IsValidFocusTarget(TeamSide sourceSide, RuntimeHero candidate)
        {
            return candidate != null
                && !candidate.IsDead
                && candidate.Side != sourceSide
                && candidate.CanBeDirectTargeted;
        }

        private static bool IsWithinRange(Vector3 sourcePosition, RuntimeHero target, float maxRange)
        {
            return target != null
                && (maxRange <= Mathf.Epsilon
                    || Vector3.Distance(sourcePosition, target.CurrentPosition) <= maxRange);
        }

        private static bool IsBetterFocusTarget(
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

        private static float ResolveCommandDuration(SkillEffectData effect)
        {
            if (effect == null)
            {
                return 0f;
            }

            var durationSeconds = Mathf.Max(0f, effect.durationSeconds);
            if (durationSeconds > Mathf.Epsilon || effect.statusEffects == null)
            {
                return durationSeconds;
            }

            for (var i = 0; i < effect.statusEffects.Count; i++)
            {
                var status = effect.statusEffects[i];
                if (status != null)
                {
                    durationSeconds = Mathf.Max(durationSeconds, status.durationSeconds);
                }
            }

            return durationSeconds;
        }

        private static StatusEffectData CreateRuntimeStatusData(StatusEffectData template, float remainingDurationSeconds)
        {
            if (template == null || template.effectType == StatusEffectType.None || remainingDurationSeconds <= Mathf.Epsilon)
            {
                return null;
            }

            var durationSeconds = template.durationSeconds > Mathf.Epsilon
                ? Mathf.Min(template.durationSeconds, remainingDurationSeconds)
                : remainingDurationSeconds;
            return new StatusEffectData
            {
                effectType = template.effectType,
                durationSeconds = Mathf.Max(0f, durationSeconds),
                magnitude = template.magnitude,
                sourceAttackPowerMultiplier = template.sourceAttackPowerMultiplier,
                targetMaxHealthMultiplier = template.targetMaxHealthMultiplier,
                activeSkillCooldownCapSeconds = template.activeSkillCooldownCapSeconds,
                tickIntervalSeconds = template.tickIntervalSeconds,
                maxStacks = template.maxStacks,
                stackGroupKey = GetFocusStackGroupKey(template),
                statusThemeKey = template.statusThemeKey,
                refreshDurationOnReapply = template.refreshDurationOnReapply,
            };
        }

        private static string GetFocusStackGroupKey(StatusEffectData status)
        {
            return status != null && !string.IsNullOrWhiteSpace(status.stackGroupKey)
                ? status.stackGroupKey
                : DefaultFocusFireStackGroupKey;
        }

        private static void RemoveExistingTeamCommands(
            BattleContext context,
            TeamSide sourceSide,
            FocusFireCommandTargetChangeReason reason)
        {
            for (var i = context.FocusFireCommands.Count - 1; i >= 0; i--)
            {
                var command = context.FocusFireCommands[i];
                if (command == null)
                {
                    context.FocusFireCommands.RemoveAt(i);
                    continue;
                }

                if (command.SourceSide != sourceSide)
                {
                    continue;
                }

                RemoveCurrentStatuses(context, command);
                PublishTargetChanged(context, command, command.CurrentTarget, null, reason);
                context.FocusFireCommands.RemoveAt(i);
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

        private static void PublishTargetChanged(
            BattleContext context,
            RuntimeFocusFireCommand command,
            RuntimeHero previousTarget,
            RuntimeHero currentTarget,
            FocusFireCommandTargetChangeReason reason)
        {
            if (context?.EventBus == null || command == null)
            {
                return;
            }

            context.EventBus.Publish(new FocusFireCommandTargetChangedEvent(
                command.Source,
                command.Skill,
                previousTarget,
                currentTarget,
                command.RemainingDurationSeconds,
                reason));
        }
    }
}
