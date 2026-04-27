using System;
using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public sealed class RuntimeBasicAttackBounceChain
    {
        private static int nextChainId;
        private readonly HashSet<string> hitTargetIds = new HashSet<string>(StringComparer.Ordinal);

        public RuntimeBasicAttackBounceChain(
            int maxAdditionalTargets,
            float searchRadius,
            float powerMultiplier,
            float projectileSpeed,
            BasicAttackEffectType effectType,
            BasicAttackTargetType targetType,
            IReadOnlyList<StatusEffectData> onHitStatusEffects,
            string primaryVariantKey,
            string visualFormKey,
            string bounceVariantKey)
        {
            ChainId = $"basic_attack_bounce_{nextChainId++:D4}";
            RemainingBounces = Mathf.Max(0, maxAdditionalTargets);
            SearchRadius = Mathf.Max(0f, searchRadius);
            PowerMultiplier = Mathf.Max(0f, powerMultiplier);
            ProjectileSpeed = Mathf.Max(0f, projectileSpeed);
            EffectType = effectType;
            TargetType = targetType;
            OnHitStatusEffects = onHitStatusEffects ?? Array.Empty<StatusEffectData>();
            PrimaryVariantKey = primaryVariantKey ?? string.Empty;
            VisualFormKey = visualFormKey ?? string.Empty;
            BounceVariantKey = bounceVariantKey ?? string.Empty;
        }

        public string ChainId { get; }

        public int RemainingBounces { get; private set; }

        public float SearchRadius { get; }

        public float PowerMultiplier { get; }

        public float ProjectileSpeed { get; }

        public BasicAttackEffectType EffectType { get; }

        public BasicAttackTargetType TargetType { get; }

        public IReadOnlyList<StatusEffectData> OnHitStatusEffects { get; }

        public string PrimaryVariantKey { get; }

        public string VisualFormKey { get; }

        public string BounceVariantKey { get; }

        public bool IsCompleted { get; private set; }

        public int BounceHitCount { get; private set; }

        public int TotalHitCount { get; private set; }

        public RuntimeHero FirstTarget { get; private set; }

        public RuntimeHero LastTarget { get; private set; }

        public bool TryRegisterHit(RuntimeHero target, bool isBounceHop)
        {
            if (target == null || string.IsNullOrWhiteSpace(target.RuntimeId) || !hitTargetIds.Add(target.RuntimeId))
            {
                return false;
            }

            FirstTarget ??= target;
            LastTarget = target;
            TotalHitCount++;
            if (isBounceHop)
            {
                BounceHitCount++;
            }

            return true;
        }

        public bool HasAlreadyHit(RuntimeHero target)
        {
            return target != null
                && !string.IsNullOrWhiteSpace(target.RuntimeId)
                && hitTargetIds.Contains(target.RuntimeId);
        }

        public bool HasRemainingBounces => RemainingBounces > 0;

        public void ConsumeBounce()
        {
            RemainingBounces = Mathf.Max(0, RemainingBounces - 1);
        }

        public void MarkCompleted()
        {
            IsCompleted = true;
        }
    }
}
