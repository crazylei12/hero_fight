using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class SkillEffectData
    {
        public SkillEffectType effectType = SkillEffectType.DirectDamage;
        [Min(0f)] public float powerMultiplier = 1f;
        [Min(0f)] public float radiusOverride = 0f;
        [Min(0f)] public float durationSeconds = 0f;
        [Min(0.1f)] public float tickIntervalSeconds = 1f;
        public bool followCaster;
        public List<StatusEffectData> statusEffects = new List<StatusEffectData>();
    }
}
