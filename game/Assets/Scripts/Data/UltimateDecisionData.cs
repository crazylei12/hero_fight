using System;

namespace Fight.Data
{
    [Serializable]
    public class UltimateDecisionData
    {
        public UltimateTargetingType targetingType = UltimateTargetingType.UseSkillTargetType;
        public UltimateConditionData primaryCondition = new UltimateConditionData();
        public UltimateConditionData secondaryCondition = new UltimateConditionData();
        public UltimateFallbackData fallback = new UltimateFallbackData();
        public UltimateConditionCombineMode combineMode = UltimateConditionCombineMode.PrimaryOnly;
    }
}
