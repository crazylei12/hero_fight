using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class StatusEffectData
    {
        public StatusEffectType effectType = StatusEffectType.None;
        [Min(0f)] public float durationSeconds = 1f;
        // Stat modifiers use decimal percentage deltas: +20% = 0.2, +200% = 2.0. Shield keeps raw value semantics.
        public float magnitude = 0f;
        // Positive values cap active-skill cooldown remaining time while the status is active.
        public float activeSkillCooldownCapSeconds = 0f;
        [Min(0.1f)] public float tickIntervalSeconds = 1f;
        [Min(1)] public int maxStacks = 1;
        public bool refreshDurationOnReapply = true;
    }
}
