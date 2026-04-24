using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class UltimateDecisionData
    {
        public UltimateTargetingType targetingType = UltimateTargetingType.UseSkillTargetType;
        [Range(0f, 1f)] public float minimumSelfHealthPercentToCast = 0f;
        public UltimateConditionData primaryCondition = new UltimateConditionData();
        public UltimateConditionData secondaryCondition = new UltimateConditionData();
        public UltimateFallbackData fallback = new UltimateFallbackData();
        public UltimateConditionCombineMode combineMode = UltimateConditionCombineMode.PrimaryOnly;
    }
}
