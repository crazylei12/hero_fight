using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

    public enum RadialSweepDirectionMode
    {
        Outward = 0,
        Inward = 1,
    }

    public enum DeployableProxySpawnMode
    {
        AtTargetPosition = 0,
        AroundTarget = 1,
        RandomForwardArea = 2,
    }

    public enum DeployableProxyTriggerMode
    {
        None = 0,
        OnOwnerBasicAttack = 1,
        PeriodicBasicAttackSequence = 2,
        PeriodicEffectPulse = 3,
        ProximityExplosion = 4,
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

    public enum ReturningPathStrikePhase
    {
        Outbound = 0,
        Return = 1,
    }

    [Serializable]
    public class CombatFormOverrideData
    {
        public string formKey = string.Empty;
        [Min(0f)] public float durationSeconds = 0f;
        public bool expiresOnDeath = true;
        public bool overrideUsesProjectile;
        public bool usesProjectile;
        [Min(0f)] public float attackRangeOverride = 0f;
        [Min(0f)] public float projectileSpeedOverride = 0f;
        public float attackPowerModifier = 0f;
        public float attackSpeedModifier = 0f;

        public bool HasAnyOverride =>
            !string.IsNullOrWhiteSpace(formKey)
            || durationSeconds > Mathf.Epsilon
            || overrideUsesProjectile
            || attackRangeOverride > Mathf.Epsilon
            || projectileSpeedOverride > Mathf.Epsilon
            || Mathf.Abs(attackPowerModifier) > Mathf.Epsilon
            || Mathf.Abs(attackSpeedModifier) > Mathf.Epsilon;
    }

    [Serializable]
    public class SkillEffectData
    {
        public SkillEffectType effectType = SkillEffectType.DirectDamage;
        public SkillEffectTargetMode targetMode = SkillEffectTargetMode.SkillTargets;
        [Min(0f)] public float powerMultiplier = 1f;
        public bool cleanseAllNegativeStatuses;
        public StatusEffectType statusStackQueryEffectType = StatusEffectType.None;
        [FormerlySerializedAs("statusStackQueryGroupKey")]
        public string statusStackQueryThemeKey = string.Empty;
        [Min(0)] public int minimumRequiredStatusStacks = 0;
        [Min(0f)] public float bonusPowerMultiplierPerStatusStack = 0f;
        public bool consumeQueriedStatusesOnHit;
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
        public GameObject areaVfxPrefabOverride;
        [Min(0f)] public float areaVfxScaleMultiplierOverride = 0f;
        public Vector3 areaVfxEulerAnglesOverride = Vector3.zero;
        public bool pulseCreatesDelayedAreaImpact;
        [Min(0f)] public float delayedAreaImpactDelaySeconds = 0f;
        [Min(0f)] public float delayedAreaImpactRadiusOverride = 0f;
        [Min(0f)] public float delayedAreaImpactPowerMultiplier = 0f;
        public GameObject delayedAreaImpactVfxPrefab;
        [Min(0f)] public float delayedAreaImpactVfxScaleMultiplierOverride = 0f;
        public Vector3 delayedAreaImpactVfxEulerAnglesOverride = Vector3.zero;
        public DeployableProxySpawnMode deployableProxySpawnMode = DeployableProxySpawnMode.AtTargetPosition;
        public DeployableProxyTriggerMode deployableProxyTriggerMode = DeployableProxyTriggerMode.None;
        [Min(0f)] public float deployableProxyStrikeRadius = 0f;
        [Min(0f)] public float deployableProxyTriggerRadius = 0f;
        [Min(0f)] public float deployableProxyEffectRadius = 0f;
        [Min(0f)] public float deployableProxySpawnOffsetDistance = 0f;
        [Min(1)] public int deployableProxySpawnCount = 1;
        public bool deployableProxyPersistUntilTriggered;
        [Min(0f)] public float deployableProxyRandomForwardMinDistance = 0f;
        [Min(0f)] public float deployableProxyRandomForwardMaxDistance = 0f;
        [Min(0f)] public float deployableProxyRandomForwardWidth = 0f;
        [Min(0f)] public float deployableProxyRandomForwardMinSpacing = 0f;
        [Min(0)] public int deployableProxyMaxCount = 0;
        public bool deployableProxyReplaceOldestWhenLimitReached = true;
        public bool deployableProxyImmediateStrikeOnSpawn;
        [Min(0f)] public float deployableProxyPowerMultiplierScale = 1f;
        [Min(0f)] public float deployableProxyAttackIntervalSeconds = 0f;
        [Min(0f)] public float deployableProxyAttackRange = 0f;
        [Min(0f)] public float deployableProxyProjectileSpeedOverride = 0f;
        [Min(0)] public int deployableProxyStartingVariantIndex = 0;
        public GameObject deployableProxySpawnVfxPrefab;
        public GameObject deployableProxyLoopVfxPrefab;
        public GameObject deployableProxyRemovalVfxPrefab;
        public Vector3 deployableProxyVfxLocalOffset = Vector3.zero;
        public Vector3 deployableProxyVfxEulerAngles = Vector3.zero;
        public Vector3 deployableProxyVfxScaleMultiplier = Vector3.one;
        [Min(0f)] public float cloneDurationSeconds = 0f;
        [Min(1)] public int cloneSpawnCount = 1;
        [Min(0)] public int cloneMaxCount = 0;
        public bool cloneReplaceOldestWhenLimitReached = true;
        [Min(0f)] public float cloneSpawnOffsetDistance = 1.1f;
        [Min(0f)] public float cloneMaxHealthMultiplier = 1f;
        [Min(0f)] public float cloneAttackPowerMultiplier = 1f;
        [Min(0f)] public float cloneDefenseMultiplier = 1f;
        [Min(0f)] public float cloneAttackSpeedMultiplier = 1f;
        [Min(0f)] public float cloneMoveSpeedMultiplier = 1f;
        [Min(0f)] public float cloneInitialActiveSkillDelaySeconds = 0.75f;
        public bool cloneExpiresWhenOwnerDies = true;
        public ReturningPathStrikePhase returningPathStrikePhase = ReturningPathStrikePhase.Outbound;
        [Min(0f)] public float returningPathMaxDistance = 0f;
        [Min(0f)] public float returningPathWidth = 0f;
        [Min(0f)] public float returningPathDelaySeconds = 0f;
        [Min(0f)] public float channeledPathMaxTurnDegreesPerSecond = 0f;
        public CombatFormOverrideData formOverride = new CombatFormOverrideData();
        public ForcedMovementDirectionMode forcedMovementDirection = ForcedMovementDirectionMode.AwayFromSource;
        [Min(0f)] public float forcedMovementDistance = 0f;
        [Min(0f)] public float forcedMovementDurationSeconds = 0f;
        [Min(0f)] public float forcedMovementPeakHeight = 0f;
        public bool repositionAwayFromPrimaryTarget;
        public bool repositionOnFarSideOfPrimaryTarget;
        public RadialSweepDirectionMode radialSweepDirection = RadialSweepDirectionMode.Outward;
        [Min(0f)] public float radialSweepStartDelaySeconds = 0f;
        [Min(0f)] public float radialSweepRingWidth = 1f;
        public List<StatusEffectData> statusEffects = new List<StatusEffectData>();
    }
}
