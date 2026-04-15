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
            hero.Tick(deltaTime);

            if (hero.IsDead)
            {
                if (hero.ReadyToRevive())
                {
                    hero.ResetToSpawn();
                    context.EventBus.Publish(new UnitRevivedEvent(hero));
                }

                return;
            }

            if (hero.HasStatus(StatusEffectType.Stun))
            {
                return;
            }

            if (context.Input.enableSkills && BattleSkillSystem.TryCastSkill(context, hero, battleManager))
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
                MoveTowardTarget(hero, currentTarget, deltaTime);
                return;
            }

            if (hero.AttackCooldownRemainingSeconds > 0f)
            {
                return;
            }

            BattleBasicAttackSystem.PerformAttack(context, hero, currentTarget, battleManager);
        }

        private static RuntimeHero SelectTargetIfNeeded(BattleContext context, RuntimeHero hero)
        {
            if (hero.CurrentTarget != null && !hero.CurrentTarget.IsDead)
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
    }
}
