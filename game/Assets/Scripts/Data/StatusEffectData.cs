using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class StatusEffectData
    {
        public StatusEffectType effectType = StatusEffectType.None;
        [Min(0f)] public float durationSeconds = 1f;
        // Stat modifiers use decimal percentage deltas: +20% = 0.2, +200% = 2.0.
        // Shield keeps raw value semantics, and DamageShare uses a 0-1 ratio such as 0.35 = 35%.
        public float magnitude = 0f;
        [Min(0f)] public float sourceAttackPowerMultiplier = 0f;
        [Min(0f)] public float targetMaxHealthMultiplier = 0f;
        // Positive values cap active-skill cooldown remaining time while the status is active.
        public float activeSkillCooldownCapSeconds = 0f;
        [Min(0.1f)] public float tickIntervalSeconds = 1f;
        [Min(1)] public int maxStacks = 1;
        // Optional same-source stacking override. This does not merge statuses across different sources.
        public string stackGroupKey = string.Empty;
        // Query-only theme key used by effects that want to read "poison", "burn", etc. across sources.
        public string statusThemeKey = string.Empty;
        public bool refreshDurationOnReapply = true;
    }
}
