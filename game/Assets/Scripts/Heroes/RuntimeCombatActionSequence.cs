using Fight.Data;
using UnityEngine;

namespace Fight.Heroes
{
    public sealed class RuntimeCombatActionSequence
    {
        private float intervalRemainingSeconds;

        public RuntimeCombatActionSequence(SkillData sourceSkill, CombatActionSequenceData definition, RuntimeHero initialTarget)
        {
            SourceSkill = sourceSkill;
            PayloadType = definition != null
                ? definition.payloadType
                : CombatActionSequencePayloadType.BasicAttack;
            TargetRefreshMode = definition != null
                ? definition.targetRefreshMode
                : CombatActionSequenceTargetRefreshMode.RefreshOnInvalid;
            InterruptFlags = definition != null
                ? definition.interruptFlags
                : CombatActionSequenceInterruptFlags.None;
            RemainingExecutions = Mathf.Max(1, definition != null ? definition.repeatCount : 1);
            IntervalSeconds = Mathf.Max(0.01f, definition != null ? definition.intervalSeconds : 0.25f);
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

        public CombatActionSequenceTargetRefreshMode TargetRefreshMode { get; }

        public CombatActionSequenceInterruptFlags InterruptFlags { get; }

        public int RemainingExecutions { get; private set; }

        public float IntervalSeconds { get; }

        public float WindupSeconds { get; }

        public float RecoverySeconds { get; }

        public float TemporaryBasicAttackRangeOverride { get; }

        public float TemporarySkillCastRangeOverride { get; }

        public RuntimeHero PreferredTarget { get; private set; }

        public bool IsReady => RemainingExecutions > 0 && intervalRemainingSeconds <= Mathf.Epsilon;

        public bool IsComplete => RemainingExecutions <= 0;

        public void Tick(float deltaTime)
        {
            intervalRemainingSeconds = Mathf.Max(0f, intervalRemainingSeconds - deltaTime);
        }

        public void UpdatePreferredTarget(RuntimeHero target)
        {
            PreferredTarget = target;
        }

        public void MarkExecutionQueued(RuntimeHero target)
        {
            PreferredTarget = target;
            RemainingExecutions = Mathf.Max(0, RemainingExecutions - 1);
            intervalRemainingSeconds = RemainingExecutions > 0
                ? IntervalSeconds
                : 0f;
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

            if ((InterruptFlags & CombatActionSequenceInterruptFlags.ForcedMovement) != 0 && owner.IsUnderForcedMovement)
            {
                return true;
            }

            return false;
        }
    }
}
