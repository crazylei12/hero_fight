using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleCombatActionSequenceSystem
    {
        public static void TryStartSequence(RuntimeHero actor, SkillData sourceSkill, RuntimeHero primaryTarget)
        {
            var definition = sourceSkill?.actionSequence;
            if (actor == null || sourceSkill == null || definition == null || !definition.enabled || !HasRemainingSequenceBudget(definition))
            {
                return;
            }

            actor.StartCombatActionSequence(new RuntimeCombatActionSequence(sourceSkill, definition, primaryTarget));
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
