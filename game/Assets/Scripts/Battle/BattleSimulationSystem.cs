using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleSimulationSystem
    {
        public static void Tick(BattleContext context, float deltaTime, BattleManager battleManager)
        {
            if (context == null || battleManager == null)
            {
                return;
            }

            BattleBasicAttackSystem.TickProjectiles(context, deltaTime, battleManager);
            TickSkillAreas(context, deltaTime, battleManager);

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                TickHero(context, context.Heroes[i], deltaTime, battleManager);
            }
        }

        private static void TickSkillAreas(BattleContext context, float deltaTime, BattleManager battleManager)
        {
            for (var i = context.SkillAreas.Count - 1; i >= 0; i--)
            {
                var area = context.SkillAreas[i];
                if (area == null)
                {
                    context.SkillAreas.RemoveAt(i);
                    continue;
                }

                var shouldPulse = area.Tick(deltaTime);
                if (shouldPulse)
                {
                    BattleSkillSystem.ResolveSkillAreaPulse(context, area, battleManager);
                }

                if (area.IsExpired)
                {
                    context.SkillAreas.RemoveAt(i);
                }
            }
        }

        private static void TickHero(BattleContext context, RuntimeHero hero, float deltaTime, BattleManager battleManager)
        {
            hero.Tick(
                deltaTime,
                status => ResolvePeriodicStatusTick(context, hero, status, battleManager),
                status => context.EventBus.Publish(new StatusRemovedEvent(status.Source, hero, status.EffectType, status.SourceSkill)));

            if (hero.IsDead)
            {
                if (hero.ReadyToRevive())
                {
                    hero.ResetToSpawn();
                    context.EventBus.Publish(new UnitRevivedEvent(hero));
                }

                return;
            }

            if (context.Input.enableSkills && hero.CanCastSkills && BattleSkillSystem.TryCastSkill(context, hero, battleManager))
            {
                return;
            }

            var currentTarget = SelectTargetIfNeeded(context, hero);
            if (currentTarget == null)
            {
                return;
            }

            if (!IsInAttackRange(hero, currentTarget))
            {
                if (hero.CanMove)
                {
                    MoveTowardTarget(hero, currentTarget, deltaTime);
                }

                return;
            }

            if (!hero.CanAttack || hero.AttackCooldownRemainingSeconds > 0f)
            {
                return;
            }

            BattleBasicAttackSystem.PerformAttack(context, hero, currentTarget, battleManager);
        }

        private static RuntimeHero SelectTargetIfNeeded(BattleContext context, RuntimeHero hero)
        {
            if (hero.CurrentTarget != null && !hero.CurrentTarget.IsDead && hero.CurrentTarget.CanBeDirectTargeted)
            {
                return hero.CurrentTarget;
            }

            var nearestEnemy = BattleAiDirector.SelectPreferredEnemyTarget(context.Heroes, hero, 999f);

            hero.SetTarget(nearestEnemy);
            context.EventBus.Publish(new TargetChangedEvent(hero, nearestEnemy));
            return nearestEnemy;
        }

        private static void MoveTowardTarget(RuntimeHero hero, RuntimeHero target, float deltaTime)
        {
            var offset = target.CurrentPosition - hero.CurrentPosition;
            if (offset.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var desiredRange = BattleAiDirector.GetDesiredCombatRange(hero);
            if (offset.magnitude <= desiredRange)
            {
                return;
            }

            var moveStep = hero.MoveSpeed * deltaTime;
            var destination = hero.CurrentPosition + offset.normalized * Mathf.Min(moveStep, Mathf.Max(0f, offset.magnitude - desiredRange));
            hero.CurrentPosition = Stage01ArenaSpec.ClampPosition(destination);
        }

        private static bool IsInAttackRange(RuntimeHero hero, RuntimeHero target)
        {
            return Vector3.Distance(hero.CurrentPosition, target.CurrentPosition) <= BattleAiDirector.GetDesiredCombatRange(hero);
        }

        private static void ResolvePeriodicStatusTick(BattleContext context, RuntimeHero target, RuntimeStatusEffect status, BattleManager battleManager)
        {
            if (context == null || target == null || status == null || battleManager == null)
            {
                return;
            }

            switch (status.EffectType)
            {
                case StatusEffectType.HealOverTime:
                    ResolveHealOverTime(context, target, status);
                    break;
                case StatusEffectType.DamageOverTime:
                    ResolveDamageOverTime(context, target, status, battleManager);
                    break;
            }
        }

        private static void ResolveHealOverTime(BattleContext context, RuntimeHero target, RuntimeStatusEffect status)
        {
            var actualHeal = target.ApplyHealing(status.Magnitude);
            if (actualHeal <= 0f)
            {
                return;
            }

            status.Source?.RecordHealing(actualHeal);
            context.EventBus.Publish(new HealAppliedEvent(status.Source ?? target, target, actualHeal, status.SourceSkill, target.CurrentHealth));
        }

        private static void ResolveDamageOverTime(BattleContext context, RuntimeHero target, RuntimeStatusEffect status, BattleManager battleManager)
        {
            var actualDamage = target.ApplyDamage(status.Magnitude);
            if (actualDamage <= 0f)
            {
                return;
            }

            status.Source?.RecordDamage(actualDamage);
            context.EventBus.Publish(new DamageAppliedEvent(
                status.Source,
                target,
                actualDamage,
                DamageSourceKind.StatusEffect,
                status.SourceSkill,
                target.CurrentHealth));

            if (target.IsDead || target.CurrentHealth <= 0f)
            {
                target.MarkDead(context.Input.respawnDelaySeconds);
                status.Source?.MarkKill();
                context.EventBus.Publish(new UnitDiedEvent(target, status.Source));

                if (status.Source != null)
                {
                    battleManager.RegisterKill(status.Source.Side);
                }
            }
        }
    }
}
