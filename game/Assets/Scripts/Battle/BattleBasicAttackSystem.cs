using Fight.Core;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleBasicAttackSystem
    {
        private const float ProjectileHitDistance = 0.1f;
        private static int projectileSequence;

        public static void TickProjectiles(BattleContext context, float deltaTime, BattleManager battleManager)
        {
            if (context == null || battleManager == null || context.Projectiles.Count == 0)
            {
                return;
            }

            for (var i = context.Projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = context.Projectiles[i];
                if (projectile.Target == null || projectile.Target.IsDead || !projectile.Target.CanBeDirectTargeted)
                {
                    context.Projectiles.RemoveAt(i);
                    continue;
                }

                var targetPosition = projectile.Target.CurrentPosition;
                var offset = targetPosition - projectile.CurrentPosition;
                var distanceToTarget = offset.magnitude;
                var step = projectile.Speed * deltaTime;

                if (distanceToTarget <= ProjectileHitDistance || step >= distanceToTarget)
                {
                    projectile.CurrentPosition = targetPosition;
                    ResolveHit(context, projectile.Attacker, projectile.Target, projectile.DamageAmount, battleManager);
                    context.Projectiles.RemoveAt(i);
                    continue;
                }

                projectile.CurrentPosition += offset.normalized * step;
            }
        }

        public static void PerformAttack(BattleContext context, RuntimeHero attacker, RuntimeHero target, BattleManager battleManager)
        {
            if (context == null || attacker == null || target == null || battleManager == null)
            {
                return;
            }

            if (!target.CanBeDirectTargeted)
            {
                return;
            }

            attacker.StartAttackCooldown();
            context.EventBus.Publish(new AttackPerformedEvent(attacker, target));

            var damage = DamageResolver.ResolveDamage(
                attacker.AttackPower,
                attacker.CriticalChance,
                attacker.CriticalDamageMultiplier,
                target.Defense,
                context.RandomService,
                attacker.Definition.basicAttack.damageMultiplier);

            if (attacker.Definition.basicAttack.usesProjectile)
            {
                LaunchProjectile(context, attacker, target, damage);
                return;
            }

            ResolveHit(context, attacker, target, damage, battleManager);
        }

        private static void LaunchProjectile(BattleContext context, RuntimeHero attacker, RuntimeHero target, float damage)
        {
            var projectileId = $"basic_attack_{projectileSequence++}";
            var projectile = new RuntimeBasicAttackProjectile(
                projectileId,
                attacker,
                target,
                attacker.CurrentPosition,
                attacker.Definition.basicAttack.projectileSpeed,
                damage);

            context.Projectiles.Add(projectile);
            context.EventBus.Publish(new BasicAttackProjectileLaunchedEvent(projectile));
        }

        private static void ResolveHit(BattleContext context, RuntimeHero attacker, RuntimeHero target, float damage, BattleManager battleManager)
        {
            if (target == null || target.IsDead || !target.CanBeDirectTargeted)
            {
                return;
            }

            var actualDamage = target.ApplyDamage(damage);
            if (actualDamage <= 0f)
            {
                return;
            }

            attacker?.RecordDamage(actualDamage);
            context.EventBus.Publish(new DamageAppliedEvent(
                attacker,
                target,
                actualDamage,
                DamageSourceKind.BasicAttack,
                null,
                target.CurrentHealth));

            if (target.IsDead || target.CurrentHealth <= 0f)
            {
                target.MarkDead(context.Input.respawnDelaySeconds);
                attacker?.MarkKill();
                context.EventBus.Publish(new UnitDiedEvent(target, attacker));

                if (attacker != null)
                {
                    battleManager.RegisterKill(attacker.Side);
                }
            }
        }
    }
}
