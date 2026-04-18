using UnityEngine;

namespace Fight.Core
{
    public static class ParabolicMotionUtility
    {
        public static float EvaluateHeightOffset(float progress, float peakHeight)
        {
            if (peakHeight <= Mathf.Epsilon)
            {
                return 0f;
            }

            var clampedProgress = Mathf.Clamp01(progress);
            return peakHeight * (4f * clampedProgress * (1f - clampedProgress));
        }
    }
}
