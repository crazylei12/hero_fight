using Fight.Heroes;
using Fight.Data;
using UnityEngine;

namespace Fight.Battle
{
    public class RuntimeBasicAttackProjectile
    {
        public RuntimeBasicAttackProjectile(
            string projectileId,
            RuntimeHero attacker,
            RuntimeHero target,
            Vector3 startPosition,
            float speed,
            float impactAmount,
            BasicAttackEffectType effectType)
        {
            ProjectileId = projectileId;
            Attacker = attacker;
            Target = target;
            CurrentPosition = startPosition;
            Speed = Mathf.Max(0.01f, speed);
            ImpactAmount = Mathf.Max(0f, impactAmount);
            EffectType = effectType;
        }

        public string ProjectileId { get; }

        public RuntimeHero Attacker { get; }

        public RuntimeHero Target { get; }

        public Vector3 CurrentPosition { get; set; }

        public float Speed { get; }

        public float ImpactAmount { get; }

        public BasicAttackEffectType EffectType { get; }
    }
}
