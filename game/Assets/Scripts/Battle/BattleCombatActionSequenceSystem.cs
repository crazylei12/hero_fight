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

        public static bool TryProgressSequence(BattleContext context, RuntimeHero actor, BattleManager battleManager)
        {
            if (context == null || actor == null || battleManager == null)
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
                CombatActionSequencePayloadType.SourceSkill => TryQueueSkillSequenceStep(context, actor, sequence, battleManager),
                _ => TryQueueBasicAttackSequenceStep(context, actor, sequence, battleManager),
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
            BattleManager battleManager)
        {
            var target = ResolveBasicAttackTarget(context, actor, sequence);
            if (target == null)
            {
                return false;
            }

            ApplySequenceTarget(context, actor, sequence, target);
            BattleBasicAttackSystem.BeginAttack(
                context,
                actor,
                target,
                battleManager,
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
            BattleManager battleManager)
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
                battleManager);
            sequence.MarkExecutionQueued(primaryTarget);

            return true;
        }

        private static RuntimeHero ResolveBasicAttackTarget(BattleContext context, RuntimeHero actor, RuntimeCombatActionSequence sequence)
        {
            if (context == null || actor?.Definition?.basicAttack == null)
            {
                return null;
            }

            if (TryResolveForcedBasicAttackTarget(actor, out var forcedTarget))
            {
                return forcedTarget;
            }

            var preferredTargetIsValid = IsValidBasicAttackTarget(actor, sequence.PreferredTarget);
            if (sequence.TargetRefreshMode != CombatActionSequenceTargetRefreshMode.RefreshEveryIteration
                && preferredTargetIsValid)
            {
                return sequence.PreferredTarget;
            }

            if (sequence.TargetRefreshMode == CombatActionSequenceTargetRefreshMode.KeepCurrentTarget)
            {
                return null;
            }

            var selected = actor.Definition.basicAttack.targetType switch
            {
                BasicAttackTargetType.LowestHealthAlly => BattleAiDirector.SelectPreferredAllyTarget(
                    context.Heroes,
                    actor,
                    actor.AttackRange,
                    allowHealthyFallback: true),
                BasicAttackTargetType.PreferredEnemy => BattleAiDirector.SelectLockedPreferredEnemyTarget(
                    context.Heroes,
                    actor,
                    sequence.PreferredTarget,
                    actor.AttackRange),
                _ => BattleAiDirector.SelectNearestEnemyTarget(context.Heroes, actor, actor.AttackRange),
            };

            return IsValidBasicAttackTarget(actor, selected)
                ? selected
                : null;
        }

        private static bool IsValidBasicAttackTarget(RuntimeHero actor, RuntimeHero target)
        {
            if (actor?.Definition?.basicAttack == null || target == null || target.IsDead || !target.CanBeDirectTargeted)
            {
                return false;
            }

            var targetMatches = actor.Definition.basicAttack.targetType switch
            {
                BasicAttackTargetType.LowestHealthAlly => target.Side == actor.Side,
                BasicAttackTargetType.PreferredEnemy => target.Side != actor.Side,
                _ => target.Side != actor.Side,
            };
            if (!targetMatches)
            {
                return false;
            }

            if (Vector3.Distance(actor.CurrentPosition, target.CurrentPosition) > actor.AttackRange)
            {
                return false;
            }

            if (actor.Definition.basicAttack.effectType == BasicAttackEffectType.Heal
                && target.CurrentHealth >= target.MaxHealth - Mathf.Epsilon)
            {
                return false;
            }

            return true;
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

            return actor.TryGetForcedEnemyTarget(out var forcedTarget)
                && !IsValidBasicAttackTarget(actor, forcedTarget);
        }

        private static bool TryResolveForcedBasicAttackTarget(RuntimeHero actor, out RuntimeHero forcedTarget)
        {
            forcedTarget = null;
            if (actor == null)
            {
                return false;
            }

            if (!actor.TryGetForcedEnemyTarget(out var resolvedForcedTarget))
            {
                return false;
            }

            if (!IsValidBasicAttackTarget(actor, resolvedForcedTarget))
            {
                return false;
            }

            forcedTarget = resolvedForcedTarget;
            return true;
        }
    }
}
