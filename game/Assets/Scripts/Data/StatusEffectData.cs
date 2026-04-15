using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class StatusEffectData
    {
        public StatusEffectType effectType = StatusEffectType.None;
        [Min(0f)] public float durationSeconds = 1f;
        public float magnitude = 0f;
        [Min(0.1f)] public float tickIntervalSeconds = 1f;
        [Min(1)] public int maxStacks = 1;
        public bool refreshDurationOnReapply = true;
    }
}
