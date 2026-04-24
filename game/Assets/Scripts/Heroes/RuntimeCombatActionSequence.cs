using Fight.Data;
using UnityEngine;

namespace Fight.Heroes
{
    public sealed class RuntimeCombatActionSequence
    {
        private readonly int maxExecutionCount;
        private float intervalRemainingSeconds;
        private bool waitingForCurrentActionToFinish;

        public RuntimeCombatActionSequence(SkillData sourceSkill, CombatActionSequenceData definition, RuntimeHero initialTarget)
        {
            SourceSkill = sourceSkill;
            PayloadType = definition != null
                ? definition.payloadType
                : CombatActionSequencePayloadType.BasicAttack;
            RepeatMode = definition != null
                ? definition.repeatMode
                : CombatActionSequenceRepeatMode.FixedCount;
            TargetRefreshMode = definition != null
                ? definition.targetRefreshMode
                : CombatActionSequenceTargetRefreshMode.RefreshOnInvalid;
            InterruptFlags = definition != null
                ? definition.interruptFlags
                : CombatActionSequenceInterruptFlags.None;
            maxExecutionCount = Mathf.Max(1, definition != null ? definition.repeatCount : 1);
            RemainingExecutions = maxExecutionCount;
            RemainingDurationSeconds = definition != null ? Mathf.Max(0.01f, definition.durationSeconds) : 1f;
            IntervalSeconds = definition != null ? Mathf.Max(0f, definition.intervalSeconds) : 0.25f;
            WindupSeconds = definition != null ? Mathf.Max(0f, definition.windupSeconds) : 0f;
            RecoverySeconds = definition != null ? Mathf.Max(0f, definition.recoverySeconds) : 0f;
            TemporaryBasicAttackRangeOverride = definition != null
                ? Mathf.Max(0f, definition.temporaryBasicAttackRangeOverride)
                : 0f;
            TemporarySkillCastRangeOverride = definition != null
                ? Mathf.Max(0f, definition.temporarySkillCastRangeOverride)
                : 0f;
            PreferredTarget = initialTarget;
            intervalRemainingSeconds = 0f;
        }

        public SkillData SourceSkill { get; }

        public CombatActionSequencePayloadType PayloadType { get; }

        public CombatActionSequenceRepeatMode RepeatMode { get; }

        public CombatActionSequenceTargetRefreshMode TargetRefreshMode { get; }

        public CombatActionSequenceInterruptFlags InterruptFlags { get; }

        public int RemainingExecutions { get; private set; }

        public float RemainingDurationSeconds { get; private set; }

        public float IntervalSeconds { get; }

        public float WindupSeconds { get; }

        public float RecoverySeconds { get; }

        public float TemporaryBasicAttackRangeOverride { get; }

        public float TemporarySkillCastRangeOverride { get; }

        public RuntimeHero PreferredTarget { get; private set; }

        public bool IsReady => HasAvailableExecutions && !waitingForCurrentActionToFinish && intervalRemainingSeconds <= Mathf.Epsilon;

        public bool IsComplete => !HasAvailableExecutions && !waitingForCurrentActionToFinish && intervalRemainingSeconds <= Mathf.Epsilon;

        public void Tick(RuntimeHero owner, float deltaTime)
        {
            var clampedDeltaTime = Mathf.Max(0f, deltaTime);
            if (RepeatMode == CombatActionSequenceRepeatMode.FixedDuration)
            {
                RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - clampedDeltaTime);
            }

            if (waitingForCurrentActionToFinish)
            {
                if (owner != null && !owner.HasPendingCombatAction && !owner.IsActionLocked)
                {
                    waitingForCurrentActionToFinish = false;
                    intervalRemainingSeconds = HasAvailableExecutions
                        ? IntervalSeconds
                        : 0f;
                }

                return;
            }

            if (!HasAvailableExecutions)
            {
                intervalRemainingSeconds = 0f;
                return;
            }

            intervalRemainingSeconds = Mathf.Max(0f, intervalRemainingSeconds - clampedDeltaTime);
        }

        public void UpdatePreferredTarget(RuntimeHero target)
        {
            PreferredTarget = target;
        }

        public void MarkExecutionQueued(RuntimeHero target)
        {
            PreferredTarget = target;
            waitingForCurrentActionToFinish = true;
            intervalRemainingSeconds = 0f;

            if (RepeatMode == CombatActionSequenceRepeatMode.FixedCount)
            {
                RemainingExecutions = Mathf.Max(0, RemainingExecutions - 1);
            }
        }

        public void MarkExecutionSkipped()
        {
            waitingForCurrentActionToFinish = false;
            if (RepeatMode == CombatActionSequenceRepeatMode.FixedCount)
            {
                RemainingExecutions = Mathf.Max(0, RemainingExecutions - 1);
            }

            intervalRemainingSeconds = HasAvailableExecutions
                ? IntervalSeconds
                : 0f;
        }

        public void RestoreQueuedExecution()
        {
            if (!waitingForCurrentActionToFinish)
            {
                return;
            }

            waitingForCurrentActionToFinish = false;
            intervalRemainingSeconds = 0f;

            if (RepeatMode == CombatActionSequenceRepeatMode.FixedCount)
            {
                RemainingExecutions = Mathf.Min(maxExecutionCount, RemainingExecutions + 1);
            }
        }

        public bool ShouldInterrupt(RuntimeHero owner)
        {
            if (owner == null)
            {
                return true;
            }

            if ((InterruptFlags & CombatActionSequenceInterruptFlags.HardControl) != 0 && owner.HasHardControl)
            {
                return true;
            }

            if ((InterruptFlags & CombatActionSequenceInterruptFlags.ForcedMovement) != 0
                && (owner.IsUnderForcedMovement || owner.HasRecentForcedMovementInterrupt))
            {
                return true;
            }

            return false;
        }

        private bool HasAvailableExecutions =>
            RepeatMode == CombatActionSequenceRepeatMode.FixedDuration
                ? RemainingDurationSeconds > Mathf.Epsilon
                : RemainingExecutions > 0;
    }
}
