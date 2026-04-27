using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class ReactiveCounterData
    {
        public bool enabled;
        [Min(0f)] public float durationSeconds = 0f;
        public bool blocksBasicAttacks = true;
        public bool blocksSkillCasts = true;
        public bool triggerOnBasicAttackDamage = true;
        public bool requireNonProjectileBasicAttack = true;
        [Min(0f)] public float sourceTriggerCooldownSeconds = 0f;
        [Min(0f)] public float counterDamagePowerMultiplier = 0f;
        [Min(0f)] public float forcedMovementDistance = 0f;
        [Min(0f)] public float forcedMovementDurationSeconds = 0f;
        [Min(0f)] public float forcedMovementPeakHeight = 0f;
        public List<StatusEffectData> onTriggerStatusEffects = new List<StatusEffectData>();

        public bool HasAnyRuntimeEffect =>
            enabled
            && durationSeconds > Mathf.Epsilon
            && (blocksBasicAttacks
                || blocksSkillCasts
                || counterDamagePowerMultiplier > Mathf.Epsilon
                || forcedMovementDistance > Mathf.Epsilon
                || forcedMovementDurationSeconds > Mathf.Epsilon
                || (onTriggerStatusEffects != null && onTriggerStatusEffects.Count > 0));
    }
}
