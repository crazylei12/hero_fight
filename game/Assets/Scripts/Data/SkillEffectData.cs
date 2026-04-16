using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    public enum PersistentAreaPulseEffectType
    {
        None = 0,
        DirectDamage = 1,
        DirectHeal = 2,
    }

    public enum PersistentAreaTargetType
    {
        Enemies = 0,
        Allies = 1,
        Both = 2,
    }

    public enum ForcedMovementDirectionMode
    {
        AwayFromSource = 0,
        TowardSource = 1,
    }

    [Serializable]
    public class SkillEffectData
    {
        public SkillEffectType effectType = SkillEffectType.DirectDamage;
        [Min(0f)] public float powerMultiplier = 1f;
        [Min(0f)] public float radiusOverride = 0f;
        [Min(0f)] public float durationSeconds = 0f;
        [Min(0.1f)] public float tickIntervalSeconds = 1f;
        public bool followCaster;
        public PersistentAreaPulseEffectType persistentAreaPulseEffectType = PersistentAreaPulseEffectType.DirectDamage;
        public PersistentAreaTargetType persistentAreaTargetType = PersistentAreaTargetType.Enemies;
        public ForcedMovementDirectionMode forcedMovementDirection = ForcedMovementDirectionMode.AwayFromSource;
        [Min(0f)] public float forcedMovementDistance = 0f;
        [Min(0f)] public float forcedMovementDurationSeconds = 0f;
        [Min(0f)] public float forcedMovementPeakHeight = 0f;
        public List<StatusEffectData> statusEffects = new List<StatusEffectData>();
    }
}
