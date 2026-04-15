using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class HeroStatsData
    {
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float attackPower = 10f;
        [Min(0f)] public float defense = 0f;
        [Min(0.01f)] public float attackSpeed = 1f;
        [Min(0.01f)] public float moveSpeed = 4f;
        [Range(0f, 1f)] public float criticalChance = 0.1f;
        [Min(1f)] public float criticalDamageMultiplier = 1.5f;
        [Min(0.1f)] public float attackRange = 1.5f;
    }
}
