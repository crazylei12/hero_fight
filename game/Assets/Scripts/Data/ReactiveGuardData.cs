using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class ReactiveGuardData
    {
        public bool enabled;
        [Min(0f)] public float durationSeconds = 0f;
        [Min(0f)] public float triggerRadius = 0f;
        [Min(0f)] public float effectRadius = 0f;
        [Min(1)] public int maxTriggerCount = 1;
        [Min(0f)] public float forcedMovementDistance = 0f;
        [Min(0f)] public float forcedMovementDurationSeconds = 0f;
        [Min(0f)] public float forcedMovementPeakHeight = 0f;
        [Min(0f)] public float healProtectedHeroPerSuccessfulKnockUp = 0f;
        public List<StatusEffectData> onTriggerStatusEffects = new List<StatusEffectData>();

        [Header("Presentation")]
        public GameObject guardLoopVfxPrefab;
        public Vector3 guardLoopVfxLocalOffset = Vector3.zero;
        public Vector3 guardLoopVfxLocalScale = Vector3.one;
        public Vector3 guardLoopVfxEulerAngles = Vector3.zero;
        public GameObject triggerVfxPrefab;
        public Vector3 triggerVfxLocalOffset = Vector3.zero;
        public Vector3 triggerVfxLocalScale = Vector3.one;
        public Vector3 triggerVfxEulerAngles = Vector3.zero;
    }
}
