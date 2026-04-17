using System;
using UnityEngine;

namespace Fight.Data
{
    public enum CombatActionSequencePayloadType
    {
        BasicAttack = 0,
        SourceSkill = 1,
    }

    public enum CombatActionSequenceRepeatMode
    {
        FixedCount = 0,
        FixedDuration = 1,
    }

    public enum CombatActionSequenceTargetRefreshMode
    {
        KeepCurrentTarget = 0,
        RefreshOnInvalid = 1,
        RefreshEveryIteration = 2,
    }

    [Flags]
    public enum CombatActionSequenceInterruptFlags
    {
        None = 0,
        HardControl = 1 << 0,
        ForcedMovement = 1 << 1,
    }

    [Serializable]
    public class CombatActionSequenceData
    {
        public bool enabled;
        public CombatActionSequencePayloadType payloadType = CombatActionSequencePayloadType.BasicAttack;
        public CombatActionSequenceRepeatMode repeatMode = CombatActionSequenceRepeatMode.FixedCount;
        [Min(1)] public int repeatCount = 1;
        [Min(0.01f)] public float durationSeconds = 1f;
        [Min(0f)] public float intervalSeconds = 0.25f;
        [Min(0f)] public float windupSeconds = 0f;
        [Min(0f)] public float recoverySeconds = 0f;
        [Min(0f)] public float temporaryBasicAttackRangeOverride = 0f;
        [Min(0f)] public float temporarySkillCastRangeOverride = 0f;
        public CombatActionSequenceTargetRefreshMode targetRefreshMode = CombatActionSequenceTargetRefreshMode.RefreshOnInvalid;
        public CombatActionSequenceInterruptFlags interruptFlags =
            CombatActionSequenceInterruptFlags.HardControl | CombatActionSequenceInterruptFlags.ForcedMovement;
    }
}
