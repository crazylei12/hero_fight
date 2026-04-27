using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleCombatActionSequenceSystem
    {
        public static void TryStartSequence(BattleContext context, RuntimeHero actor, SkillData sourceSkill, RuntimeHero primaryTarget)
        {
            var definition = sourceSkill?.actionSequence;
            if (actor == null || sourceSkill == null || definition == null || !definition.enabled || !HasRemainingSequenceBudget(definition))
            {
                return;
            }

            var targetSnapshot = CreateUniqueTargetSnapshot(context, actor, sourceSkill, definition);
            actor.StartCombatActionSequence(new RuntimeCombatActionSequence(sourceSkill, definition, primaryTarget, targetSnapshot));
        }

        public static bool TryProgressSequence(BattleContext context, RuntimeHero actor, IBattleSimulationCallbacks battleCallbacks)
        {
            if (context == null || actor == null || battleCallbacks == null)
            {
                return false;
            }

            var sequence = actor.ActiveCombatActionSequence;
            if (sequence == null)
            {
                return false;
            }

            if (sequence.ShouldInterrupt(actor))
            {
                actor.ClearCombatActionSequence();
                return false;
            }

            if (sequence.IsComplete)
            {
                actor.ClearCombatActionSequence();
                return false;
            }

            if (ShouldEndSequenceBeforeExecution(actor, sequence))
            {
                actor.ClearCombatActionSequence();
                return false;
            }

            if (!sequence.IsReady)
            {
                return true;
            }

            if (!CanExecuteSequencePayload(actor, sequence))
            {
                return true;
            }

            var queued = sequence.PayloadType switch
            {
                CombatActionSequencePayloadType.SourceSkill => TryQueueSkillSequenceStep(context, actor, sequence, battleCallbacks),
                _ => TryQueueBasicAttackSequenceStep(context, actor, sequence, battleCallbacks),
            };

            if (!queued)
            {
                actor.ClearCombatActionSequence();
                return false;
            }

            return true;
        }

        private static bool TryQueueBasicAttackSequenceStep(
            BattleContext context,
            RuntimeHero actor,
            RuntimeCombatActionSequence sequence,
            IBattleSimulationCallbacks battleCallbacks)
        {
            if (!BattleBasicAttackSystem.TryResolveHeroAttack(
                    context,
                    actor,
                    actor.AttackRange,
                    out var target,
                    out var resolvedAttack))
            {
                return false;
            }

            ApplySequenceTarget(context, actor, sequence, target);
            BattleBasicAttackSystem.BeginAttack(
                context,
                actor,
                target,
                resolvedAttack,
                battleCallbacks,
                sequence.WindupSeconds,
                sequence.RecoverySeconds,
                consumeAttackCooldown: false,
                isActionSequenceStep: true);
            sequence.MarkExecutionQueued(target);

            return true;
        }

        private static bool TryQueueSkillSequenceStep(
            BattleContext context,
            RuntimeHero actor,
            RuntimeCombatActionSequence sequence,
            IBattleSimulationCallbacks battleCallbacks)
        {
            if (!BattleSkillSystem.TryPrepareSequenceSkillCast(
                    context,
                    actor,
                    sequence.SourceSkill,
                    sequence.PreferredTarget,
                    sequence.TargetRefreshMode,
                    sequence.TemporarySkillCastRangeOverride,
                    sequence,
                    out var primaryTarget,
                    out var affectedTargets))
            {
                if (ShouldSkipEmptyGlobalSkillPulse(sequence))
                {
                    sequence.MarkExecutionSkipped();
                    return true;
                }

                return false;
            }

            ApplySequenceTarget(context, actor, sequence, primaryTarget);
            BattleSkillSystem.BeginSequenceSkillCast(
                context,
                actor,
                sequence.SourceSkill,
                primaryTarget,
                affectedTargets,
                sequence.WindupSeconds,
                sequence.RecoverySeconds,
                battleCallbacks);
            sequence.MarkExecutionQueued(primaryTarget);

            return true;
        }

        private static bool ShouldSkipEmptyGlobalSkillPulse(RuntimeCombatActionSequence sequence)
        {
            var targetType = sequence?.SourceSkill?.targetType ?? SkillTargetType.None;
            return targetType == SkillTargetType.AllEnemies || targetType == SkillTargetType.AllAllies;
        }

        private static void ApplySequenceTarget(
            BattleContext context,
            RuntimeHero actor,
            RuntimeCombatActionSequence sequence,
            RuntimeHero target)
        {
            sequence.UpdatePreferredTarget(target);
            if (actor.CurrentTarget == target)
            {
                return;
            }

            actor.SetTarget(target);
            context.EventBus.Publish(new TargetChangedEvent(actor, target));
        }

        private static bool HasRemainingSequenceBudget(CombatActionSequenceData definition)
        {
            if (definition == null)
            {
                return false;
            }

            return definition.repeatMode == CombatActionSequenceRepeatMode.FixedDuration
                ? definition.durationSeconds > 0f
                : definition.repeatCount > 0;
        }

        private static bool CanExecuteSequencePayload(RuntimeHero actor, RuntimeCombatActionSequence sequence)
        {
            if (actor == null || sequence == null)
            {
                return false;
            }

            return sequence.PayloadType == CombatActionSequencePayloadType.SourceSkill
                ? actor.CanCastSkills
                : actor.CanAttack;
        }

        private static IReadOnlyList<RuntimeHero> CreateUniqueTargetSnapshot(
            BattleContext context,
            RuntimeHero actor,
            SkillData sourceSkill,
            CombatActionSequenceData definition)
        {
            var results = new List<RuntimeHero>();
            if (context?.Heroes == null
                || actor == null
                || sourceSkill == null
                || definition == null
                || definition.targetRefreshMode != CombatActionSequenceTargetRefreshMode.RefreshEveryIterationUniqueTarget)
            {
                return null;
            }

            var maxRange = definition.temporarySkillCastRangeOverride > Mathf.Epsilon
                ? Mathf.Max(sourceSkill.castRange, definition.temporarySkillCastRangeOverride)
                : sourceSkill.castRange;
            var minimumDistance = Mathf.Max(0f, sourceSkill.minimumTargetDistance);
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate == null
                    || candidate.IsDead
                    || candidate.Side == actor.Side
                    || !candidate.CanBeDirectTargeted)
                {
                    continue;
                }

                var distance = Vector3.Distance(actor.CurrentPosition, candidate.CurrentPosition);
                if (distance > maxRange || distance + Mathf.Epsilon < minimumDistance)
                {
                    continue;
                }

                results.Add(candidate);
            }

            results.Sort((left, right) => CompareUniqueTargetOrder(actor, left, right));
            return results;
        }

        private static int CompareUniqueTargetOrder(RuntimeHero actor, RuntimeHero left, RuntimeHero right)
        {
            if (left == right)
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

            var leftDistance = Vector3.Distance(actor.CurrentPosition, left.CurrentPosition);
            var rightDistance = Vector3.Distance(actor.CurrentPosition, right.CurrentPosition);
            if (Mathf.Abs(leftDistance - rightDistance) > Mathf.Epsilon)
            {
                return rightDistance.CompareTo(leftDistance);
            }

            if (Mathf.Abs(left.CurrentHealth - right.CurrentHealth) > Mathf.Epsilon)
            {
                return left.CurrentHealth.CompareTo(right.CurrentHealth);
            }

            var leftRatio = left.MaxHealth > Mathf.Epsilon ? left.CurrentHealth / left.MaxHealth : 1f;
            var rightRatio = right.MaxHealth > Mathf.Epsilon ? right.CurrentHealth / right.MaxHealth : 1f;
            if (Mathf.Abs(leftRatio - rightRatio) > Mathf.Epsilon)
            {
                return leftRatio.CompareTo(rightRatio);
            }

            return left.SlotIndex.CompareTo(right.SlotIndex);
        }

        private static bool ShouldEndSequenceBeforeExecution(RuntimeHero actor, RuntimeCombatActionSequence sequence)
        {
            if (actor == null || sequence == null)
            {
                return true;
            }

            if (sequence.PayloadType == CombatActionSequencePayloadType.SourceSkill)
            {
                return !actor.CanCastSkills;
            }

            return false;
        }
    }
}
