using System.Collections.Generic;
using Fight.Data;
using UnityEngine;

namespace Fight.Battle
{
    public sealed class ResolvedBasicAttack
    {
        public ResolvedBasicAttack(
            string variantKey,
            BasicAttackEffectType effectType,
            BasicAttackTargetType targetType,
            float powerMultiplier,
            float targetPrioritySearchRadius,
            bool usesProjectile,
            float projectileSpeed,
            IReadOnlyList<StatusEffectData> onHitStatusEffects,
            Vector3 launchPosition,
            bool advanceSequenceOnUse)
        {
            VariantKey = variantKey ?? string.Empty;
            EffectType = effectType;
            TargetType = targetType;
            PowerMultiplier = Mathf.Max(0f, powerMultiplier);
            TargetPrioritySearchRadius = Mathf.Max(0f, targetPrioritySearchRadius);
            UsesProjectile = usesProjectile;
            ProjectileSpeed = Mathf.Max(0f, projectileSpeed);
            OnHitStatusEffects = onHitStatusEffects ?? System.Array.Empty<StatusEffectData>();
            LaunchPosition = launchPosition;
            AdvanceSequenceOnUse = advanceSequenceOnUse;
        }

        public string VariantKey { get; }

        public BasicAttackEffectType EffectType { get; }

        public BasicAttackTargetType TargetType { get; }

        public float PowerMultiplier { get; }

        public float TargetPrioritySearchRadius { get; }

        public bool UsesProjectile { get; }

        public float ProjectileSpeed { get; }

        public IReadOnlyList<StatusEffectData> OnHitStatusEffects { get; }

        public Vector3 LaunchPosition { get; }

        public bool AdvanceSequenceOnUse { get; }
    }
}
