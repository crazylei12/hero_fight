using Fight.Heroes;
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
            float damageAmount)
        {
            ProjectileId = projectileId;
            Attacker = attacker;
            Target = target;
            CurrentPosition = startPosition;
            Speed = Mathf.Max(0.01f, speed);
            DamageAmount = Mathf.Max(0f, damageAmount);
        }

        public string ProjectileId { get; }

        public RuntimeHero Attacker { get; }

        public RuntimeHero Target { get; }

        public Vector3 CurrentPosition { get; set; }

        public float Speed { get; }

        public float DamageAmount { get; }
    }
}
