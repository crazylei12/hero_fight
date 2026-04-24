using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    public enum BasicAttackEffectType
    {
        Damage = 0,
        Heal = 1,
    }

    public enum BasicAttackTargetType
    {
        NearestEnemy = 0,
        LowestHealthAlly = 1,
        ThreateningEnemyNearRangedAlly = 2,
    }

    [Serializable]
    public class BasicAttackVariantData
    {
        public string variantKey = string.Empty;
        public BasicAttackEffectType effectType = BasicAttackEffectType.Damage;
        public BasicAttackTargetType targetType = BasicAttackTargetType.NearestEnemy;
        [Min(0f)] public float powerMultiplier = 1f;
        [Min(0f)] public float targetPrioritySearchRadius = 0f;
        [Min(-1)] public int missingTargetFallbackVariantIndex = -1;
        public List<StatusEffectData> onHitStatusEffects = new List<StatusEffectData>();
    }

    [Serializable]
    public class BasicAttackBounceData
    {
        [Min(0)] public int maxAdditionalTargets = 0;
        [Min(0f)] public float searchRadius = 0f;
        [Min(0f)] public float powerMultiplier = 0f;
        public string bounceVariantKey = string.Empty;
    }

    [Serializable]
    public class BasicAttackData
    {
        [Min(0.1f)] public float damageMultiplier = 1f;
        // Legacy compatibility field. Runtime attack cadence now derives from HeroStatsData.attackSpeed.
        [Min(0.05f)] public float attackInterval = 1f;
        [Min(0f)] public float rangeOverride = 0f;
        public bool usesProjectile;
        [Min(0f)] public float projectileSpeed = 0f;
        public BasicAttackEffectType effectType = BasicAttackEffectType.Damage;
        public BasicAttackTargetType targetType = BasicAttackTargetType.NearestEnemy;
        [Min(0f)] public float targetPrioritySearchRadius = 0f;
        [Min(0)] public int startingVariantIndex = 0;
        public BasicAttackBounceData bounce = new BasicAttackBounceData();
        public List<StatusEffectData> onHitStatusEffects = new List<StatusEffectData>();
        public List<BasicAttackVariantData> variants = new List<BasicAttackVariantData>();
    }
}
