using Fight.Core;
using Fight.Data;
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
                if (!IsValidTarget(projectile.Attacker, projectile.Target))
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
                    ResolveHit(context, projectile.Attacker, projectile.Target, projectile.ImpactAmount, projectile.EffectType, battleManager);
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

            if (!IsValidTarget(attacker, target))
            {
                return;
            }

            var basicAttack = attacker.Definition.basicAttack;
            var effectType = basicAttack.effectType;
            if (!CanApplyEffectToTarget(target, effectType))
            {
                return;
            }

            attacker.StartAttackCooldown();
            context.EventBus.Publish(new AttackPerformedEvent(attacker, target));

            var impactAmount = effectType == BasicAttackEffectType.Heal
                ? HealResolver.ResolveHealAmount(attacker, basicAttack.damageMultiplier)
                : DamageResolver.ResolveDamage(
                    attacker.AttackPower,
                    attacker.CriticalChance,
                    attacker.CriticalDamageMultiplier,
                    target.Defense,
                    context.RandomService,
                    basicAttack.damageMultiplier);

            if (basicAttack.usesProjectile)
            {
                LaunchProjectile(context, attacker, target, impactAmount, effectType);
                return;
            }

            ResolveHit(context, attacker, target, impactAmount, effectType, battleManager);
        }

        private static void LaunchProjectile(BattleContext context, RuntimeHero attacker, RuntimeHero target, float impactAmount, BasicAttackEffectType effectType)
        {
            var projectileId = $"basic_attack_{projectileSequence++}";
            var projectile = new RuntimeBasicAttackProjectile(
                projectileId,
                attacker,
                target,
                attacker.CurrentPosition,
                attacker.Definition.basicAttack.projectileSpeed,
                impactAmount,
                effectType);

            context.Projectiles.Add(projectile);
            context.EventBus.Publish(new BasicAttackProjectileLaunchedEvent(projectile));
        }

        private static void ResolveHit(BattleContext context, RuntimeHero attacker, RuntimeHero target, float impactAmount, BasicAttackEffectType effectType, BattleManager battleManager)
        {
            if (!IsValidTarget(attacker, target) || !CanApplyEffectToTarget(target, effectType))
            {
                return;
            }

            if (effectType == BasicAttackEffectType.Heal)
            {
                var actualHeal = target.ApplyHealing(impactAmount);
                if (actualHeal <= 0f)
                {
                    return;
                }

                attacker?.RecordHealing(actualHeal);
                context.EventBus.Publish(new HealAppliedEvent(attacker, target, actualHeal, null, target.CurrentHealth));
                return;
            }

            var actualDamage = target.ApplyDamage(
                impactAmount,
                status => PublishStatusRemovedEvent(context, target, status));
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

        private static bool CanApplyEffectToTarget(RuntimeHero target, BasicAttackEffectType effectType)
        {
            if (target == null)
            {
                return false;
            }

            return effectType != BasicAttackEffectType.Heal
                || target.CurrentHealth < target.MaxHealth - Mathf.Epsilon;
        }

        private static bool IsValidTarget(RuntimeHero attacker, RuntimeHero target)
        {
            if (attacker?.Definition == null || target == null || target.IsDead)
            {
                return false;
            }

            return attacker.Definition.basicAttack.targetType switch
            {
                BasicAttackTargetType.LowestHealthAlly => target.Side == attacker.Side && target.CanBeDirectTargeted,
                _ => target.Side != attacker.Side && target.CanBeDirectTargeted,
            };
        }

        private static void PublishStatusRemovedEvent(BattleContext context, RuntimeHero target, RuntimeStatusEffect status)
        {
            if (context?.EventBus == null || target == null || status == null)
            {
                return;
            }

            context.EventBus.Publish(new StatusRemovedEvent(status.Source, target, status.EffectType, status.SourceSkill));
        }
    }
}
