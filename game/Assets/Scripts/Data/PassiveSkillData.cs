using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class PassiveSkillData
    {
        [Min(0f)] public float missingHealthAttackPowerRatio = 0f;
        [Min(0f)] public float maxAttackPowerBonus = 0f;

        public bool HasMissingHealthAttackPowerBonus =>
            missingHealthAttackPowerRatio > Mathf.Epsilon
            && maxAttackPowerBonus > Mathf.Epsilon;
    }
}
