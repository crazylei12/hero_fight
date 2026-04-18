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
        PreferredEnemy = 2,
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
        public List<StatusEffectData> onHitStatusEffects = new List<StatusEffectData>();
    }
}
