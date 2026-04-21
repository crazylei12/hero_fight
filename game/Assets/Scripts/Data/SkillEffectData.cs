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

    public enum SkillEffectTargetMode
    {
        SkillTargets = 0,
        Caster = 1,
        PrimaryTarget = 2,
        EnemiesInRadiusAroundCaster = 3,
        AlliesInRadiusAroundCaster = 4,
        DashPathEnemies = 5,
        EnemiesInRadiusAroundPrimaryTarget = 6,
        AlliesInRadiusAroundPrimaryTarget = 7,
        OtherAlliesInRadiusAroundCaster = 8,
    }

    [Serializable]
    public class SkillEffectData
    {
        public SkillEffectType effectType = SkillEffectType.DirectDamage;
        public SkillEffectTargetMode targetMode = SkillEffectTargetMode.SkillTargets;
        [Min(0f)] public float powerMultiplier = 1f;
        public StatusEffectType statusStackQueryEffectType = StatusEffectType.None;
        public string statusStackQueryGroupKey = string.Empty;
        [Min(0)] public int minimumRequiredStatusStacks = 0;
        [Min(0f)] public float bonusPowerMultiplierPerStatusStack = 0f;
        [Min(0f)] public float radiusOverride = 0f;
        [Min(0f)] public float durationSeconds = 0f;
        [Min(0.1f)] public float tickIntervalSeconds = 1f;
        public bool followCaster;
        public PersistentAreaPulseEffectType persistentAreaPulseEffectType = PersistentAreaPulseEffectType.DirectDamage;
        public PersistentAreaTargetType persistentAreaTargetType = PersistentAreaTargetType.Enemies;
        public bool triggerFollowUpAreaOnTargetDeath;
        [Min(0f)] public float followUpAreaRadius = 0f;
        [Min(0f)] public float followUpAreaPowerMultiplier = 0f;
        public List<StatusEffectData> followUpAreaStatusEffects = new List<StatusEffectData>();
        public bool followUpAreaCanChain = true;
        public bool followUpAreaLimitTriggerOncePerUnitPerExecution = true;
        public ForcedMovementDirectionMode forcedMovementDirection = ForcedMovementDirectionMode.AwayFromSource;
        [Min(0f)] public float forcedMovementDistance = 0f;
        [Min(0f)] public float forcedMovementDurationSeconds = 0f;
        [Min(0f)] public float forcedMovementPeakHeight = 0f;
        public List<StatusEffectData> statusEffects = new List<StatusEffectData>();
    }
}
