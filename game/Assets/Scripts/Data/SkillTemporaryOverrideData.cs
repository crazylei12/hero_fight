using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class SkillTemporaryOverrideData
    {
        [Min(0f)] public float durationSeconds = 0f;
        public SkillTemporaryOverrideLifestealMode lifestealMode = SkillTemporaryOverrideLifestealMode.Additive;
        [Min(0f)] public float lifestealRatio = 0f;
        [Min(1f)] public float visualScaleMultiplier = 1f;
        public Color visualTintColor = Color.white;
        [Range(0f, 1f)] public float visualTintStrength = 0f;

        public bool HasAnyOverride =>
            durationSeconds > Mathf.Epsilon
            && (lifestealRatio > Mathf.Epsilon
                || visualScaleMultiplier > 1f + Mathf.Epsilon
                || visualTintStrength > Mathf.Epsilon);
    }
}
