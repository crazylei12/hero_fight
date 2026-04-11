using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class UltimateFallbackData
    {
        public UltimateFallbackType fallbackType = UltimateFallbackType.None;
        [Min(0f)] public float triggerAfterSeconds = 0f;

        public int overrideRequiredUnitCount = 0;
        public float overrideHealthPercentThreshold = -1f;

        [Min(0f)] public float secondaryTriggerAfterSeconds = 0f;
        public int secondaryOverrideRequiredUnitCount = 0;
        public float secondaryOverrideHealthPercentThreshold = -1f;
    }
}
