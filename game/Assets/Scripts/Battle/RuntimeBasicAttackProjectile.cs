using Fight.Heroes;
using Fight.Data;
using UnityEngine;
using System.Collections.Generic;

namespace Fight.Battle
{
    public class RuntimeBasicAttackProjectile
    {
        public RuntimeBasicAttackProjectile(
            string projectileId,
            RuntimeHero attacker,
            RuntimeDeployableProxy sourceProxy,
            RuntimeHero target,
            Vector3 startPosition,
            float speed,
            float impactAmount,
            BasicAttackEffectType effectType,
            string variantKey,
            BasicAttackTargetType targetType,
            BasicAttackSameTargetStackData sameTargetStacking,
            IReadOnlyList<StatusEffectData> onHitStatusEffects,
            RuntimeBasicAttackBounceChain bounceChain,
            int bounceHopIndex)
        {
            ProjectileId = projectileId;
            Attacker = attacker;
            SourceProxy = sourceProxy;
            Target = target;
            CurrentPosition = startPosition;
            Speed = Mathf.Max(0.01f, speed);
            ImpactAmount = Mathf.Max(0f, impactAmount);
            EffectType = effectType;
            VariantKey = variantKey ?? string.Empty;
            TargetType = targetType;
            SameTargetStacking = sameTargetStacking;
            OnHitStatusEffects = onHitStatusEffects ?? System.Array.Empty<StatusEffectData>();
            BounceChain = bounceChain;
            BounceHopIndex = Mathf.Max(0, bounceHopIndex);
        }

        public string ProjectileId { get; }

        public RuntimeHero Attacker { get; }

        public RuntimeDeployableProxy SourceProxy { get; }

        public RuntimeHero Target { get; }

        public Vector3 CurrentPosition { get; set; }

        public float Speed { get; }

        public float ImpactAmount { get; }

        public BasicAttackEffectType EffectType { get; }

        public string VariantKey { get; }

        public BasicAttackTargetType TargetType { get; }

        public BasicAttackSameTargetStackData SameTargetStacking { get; }

        public IReadOnlyList<StatusEffectData> OnHitStatusEffects { get; }

        public RuntimeBasicAttackBounceChain BounceChain { get; }

        public int BounceHopIndex { get; }
    }
}
