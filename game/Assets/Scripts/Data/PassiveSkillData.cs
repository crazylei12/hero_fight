using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class PassiveSkillData
    {
        [Min(0f)] public float missingHealthAttackPowerRatio = 0f;
        [Min(0f)] public float maxAttackPowerBonus = 0f;
        [Min(0f)] public float recentDirectHostileSourceWindowSeconds = 0f;
        [Min(0f)] public float recentDirectHostileSourceDefenseBonusPerSource = 0f;
        [Min(0f)] public float maxDefenseBonus = 0f;

        public bool HasMissingHealthAttackPowerBonus =>
            missingHealthAttackPowerRatio > Mathf.Epsilon
            && maxAttackPowerBonus > Mathf.Epsilon;

        public bool HasRecentDirectHostileSourceDefenseBonus =>
            recentDirectHostileSourceWindowSeconds > Mathf.Epsilon
            && recentDirectHostileSourceDefenseBonusPerSource > Mathf.Epsilon
            && maxDefenseBonus > Mathf.Epsilon;
    }
}
