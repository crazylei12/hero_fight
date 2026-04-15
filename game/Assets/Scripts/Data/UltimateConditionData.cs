using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class UltimateConditionData
    {
        public UltimateConditionType conditionType = UltimateConditionType.None;

        [Min(0f)] public float searchRadius = 0f;
        [Min(0)] public int requiredUnitCount = 1;
        [Range(0f, 1f)] public float healthPercentThreshold = 1f;
        [Min(0f)] public float durationSeconds = 0f;

        public HighValueTargetType highValueTargetType = HighValueTargetType.None;
        public bool requireTargetInCastRange = true;
    }
}
