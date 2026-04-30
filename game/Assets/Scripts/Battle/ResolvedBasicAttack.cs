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
            string visualFormKey,
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
            VisualFormKey = visualFormKey ?? string.Empty;
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

        public string VisualFormKey { get; }

        public int MaxAdditionalBounceTargets { get; }

        public float BounceSearchRadius { get; }

        public float BouncePowerMultiplier { get; }

        public string BounceVariantKey { get; }

        public bool HasBounce => MaxAdditionalBounceTargets > 0 && BounceSearchRadius > Mathf.Epsilon && BouncePowerMultiplier > Mathf.Epsilon;

        public BasicAttackSameTargetStackData SameTargetStacking { get; }

        public IReadOnlyList<StatusEffectData> OnHitStatusEffects { get; }

        public Vector3 LaunchPosition { get; }

        public bool AdvanceSequenceOnUse { get; }

        public ResolvedBasicAttack WithTargetSwitchTrigger(BasicAttackTargetSwitchTriggerData triggerData)
        {
            if (triggerData == null || !triggerData.HasAnyEffect)
            {
                return this;
            }

            return new ResolvedBasicAttack(
                string.IsNullOrWhiteSpace(triggerData.variantKey) ? VariantKey : triggerData.variantKey,
                EffectType,
                TargetType,
                triggerData.powerMultiplier > Mathf.Epsilon ? triggerData.powerMultiplier : PowerMultiplier,
                TargetPrioritySearchRadius,
                UsesProjectile,
                ProjectileSpeed,
                VisualFormKey,
                MaxAdditionalBounceTargets,
                BounceSearchRadius,
                BouncePowerMultiplier,
                BounceVariantKey,
                SameTargetStacking,
                MergeOnHitStatusEffects(OnHitStatusEffects, triggerData.onHitStatusEffects),
                LaunchPosition,
                AdvanceSequenceOnUse);
        }

        private static IReadOnlyList<StatusEffectData> MergeOnHitStatusEffects(
            IReadOnlyList<StatusEffectData> baseEffects,
            IReadOnlyList<StatusEffectData> triggerEffects)
        {
            if (triggerEffects == null || triggerEffects.Count == 0)
            {
                return baseEffects ?? System.Array.Empty<StatusEffectData>();
            }

            if (baseEffects == null || baseEffects.Count == 0)
            {
                return triggerEffects;
            }

            var mergedEffects = new List<StatusEffectData>(baseEffects.Count + triggerEffects.Count);
            for (var i = 0; i < baseEffects.Count; i++)
            {
                mergedEffects.Add(baseEffects[i]);
            }

            for (var i = 0; i < triggerEffects.Count; i++)
            {
                mergedEffects.Add(triggerEffects[i]);
            }

            return mergedEffects;
        }
    }
}
