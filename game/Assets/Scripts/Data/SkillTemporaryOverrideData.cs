using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class SkillTemporaryOverrideData
    {
        [Min(0f)] public float durationSeconds = 0f;
        [Min(0f)] public float lifestealRatio = 0f;
        [Min(1f)] public float visualScaleMultiplier = 1f;

        public bool HasAnyOverride =>
            durationSeconds > Mathf.Epsilon
            && (lifestealRatio > Mathf.Epsilon || visualScaleMultiplier > 1f + Mathf.Epsilon);
    }
}
