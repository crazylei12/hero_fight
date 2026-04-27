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
            int maxAdditionalBounceTargets,
            float bounceSearchRadius,
            float bouncePowerMultiplier,
            string bounceVariantKey,
            BasicAttackSameTargetStackData sameTargetStacking,
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
            MaxAdditionalBounceTargets = Mathf.Max(0, maxAdditionalBounceTargets);
            BounceSearchRadius = Mathf.Max(0f, bounceSearchRadius);
            BouncePowerMultiplier = Mathf.Max(0f, bouncePowerMultiplier);
            BounceVariantKey = bounceVariantKey ?? string.Empty;
            SameTargetStacking = sameTargetStacking;
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

        public int MaxAdditionalBounceTargets { get; }

        public float BounceSearchRadius { get; }

        public float BouncePowerMultiplier { get; }

        public string BounceVariantKey { get; }

        public bool HasBounce => MaxAdditionalBounceTargets > 0 && BounceSearchRadius > Mathf.Epsilon && BouncePowerMultiplier > Mathf.Epsilon;

        public BasicAttackSameTargetStackData SameTargetStacking { get; }

        public IReadOnlyList<StatusEffectData> OnHitStatusEffects { get; }

        public Vector3 LaunchPosition { get; }

        public bool AdvanceSequenceOnUse { get; }
    }
}
