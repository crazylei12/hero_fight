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

                area.Tick(deltaTime);

                var pendingPulseCount = area.ConsumePendingPulseCount();
                for (var pulseIndex = 0; pulseIndex < pendingPulseCount; pulseIndex++)
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
                status => PublishStatusRemovedEvent(context, hero, status));

            if (hero.IsDead)
            {
                if (hero.ReadyToRevive())
                {
                    hero.ResetToSpawn();
                    context.EventBus.Publish(new UnitRevivedEvent(hero));
                }

                return;
            }

            if (hero.IsUnderForcedMovement)
            {
                return;
            }

            if (context.Input.enableSkills && hero.CanCastSkills && BattleSkillSystem.TryCastSkill(context, hero, battleManager))
            {
                return;
            }

            var currentTarget = SelectTargetIfNeeded(context, hero);
            if (currentTarget == null)
            {
                hero.StopThreatRetreat();
                return;
            }

            if (TryRetreatFromRecentThreat(context, hero, currentTarget, deltaTime))
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

            if (!ShouldPerformBasicAttack(hero, currentTarget))
            {
                return;
            }

            BattleBasicAttackSystem.PerformAttack(context, hero, currentTarget, battleManager);
        }

        private static bool TryRetreatFromRecentThreat(BattleContext context, RuntimeHero hero, RuntimeHero currentTarget, float deltaTime)
        {
            if (context?.Clock == null || hero == null || currentTarget == null || !hero.CanMove)
            {
                hero?.StopThreatRetreat();
                return false;
            }

            if (!BattleAiDirector.ShouldRetreatFromRecentThreat(hero, currentTarget, context.Clock.ElapsedTimeSeconds, out var threat))
            {
                hero.StopThreatRetreat();
                return false;
            }

            hero.StartThreatRetreat(threat);
            MoveAwayFromThreat(hero, threat, deltaTime);
            return true;
        }

        private static RuntimeHero SelectTargetIfNeeded(BattleContext context, RuntimeHero hero)
        {
            if (!RequiresBasicAttackRetarget(hero) && IsCurrentTargetValid(hero, hero.CurrentTarget))
            {
                return hero.CurrentTarget;
            }

            var nextTarget = SelectPreferredBasicAttackTarget(context, hero);
            if (nextTarget != hero.CurrentTarget)
            {
                hero.SetTarget(nextTarget);
                context.EventBus.Publish(new TargetChangedEvent(hero, nextTarget));
            }
            else
            {
                hero.SetTarget(nextTarget);
            }

            return nextTarget;
        }

        private static RuntimeHero SelectPreferredBasicAttackTarget(BattleContext context, RuntimeHero hero)
        {
            if (context == null || hero?.Definition == null)
            {
                return null;
            }

            return hero.Definition.basicAttack.targetType switch
            {
                BasicAttackTargetType.LowestHealthAlly => BattleAiDirector.SelectPreferredAllyTarget(context.Heroes, hero, 999f, allowHealthyFallback: true),
                _ => BattleAiDirector.SelectPreferredEnemyTarget(context.Heroes, hero, 999f),
            };
        }

        private static bool RequiresBasicAttackRetarget(RuntimeHero hero)
        {
            return hero?.Definition?.basicAttack.targetType == BasicAttackTargetType.LowestHealthAlly;
        }

        private static bool ShouldPerformBasicAttack(RuntimeHero hero, RuntimeHero target)
        {
            if (hero?.Definition?.basicAttack == null || target == null)
            {
                return false;
            }

            if (hero.Definition.basicAttack.effectType != BasicAttackEffectType.Heal)
            {
                return true;
            }

            return target.CurrentHealth < target.MaxHealth - Mathf.Epsilon;
        }

        private static bool IsCurrentTargetValid(RuntimeHero hero, RuntimeHero target)
        {
            if (hero?.Definition == null || target == null || target.IsDead)
            {
                return false;
            }

            return hero.Definition.basicAttack.targetType switch
            {
                BasicAttackTargetType.LowestHealthAlly => target.Side == hero.Side && target.CanBeDirectTargeted,
                _ => target.Side != hero.Side && target.CanBeDirectTargeted,
            };
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

        private static void MoveAwayFromThreat(RuntimeHero hero, RuntimeHero threat, float deltaTime)
        {
            var retreatDirection = BattleAiDirector.GetThreatRetreatDirection(hero, threat);
            if (retreatDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var moveStep = hero.MoveSpeed * deltaTime;
            var desiredDistance = BattleAiDirector.GetDesiredCombatRange(hero) + 0.2f;
            var currentDistance = Vector3.Distance(hero.CurrentPosition, threat.CurrentPosition);
            var distanceToGain = Mathf.Max(moveStep * 0.5f, desiredDistance - currentDistance);
            var destination = hero.CurrentPosition + retreatDirection * Mathf.Min(moveStep, distanceToGain);
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
            var actualDamage = target.ApplyDamage(
                status.Magnitude,
                expiredStatus => PublishStatusRemovedEvent(context, target, expiredStatus));
            if (actualDamage <= 0f)
            {
                return;
            }

            status.Source?.RecordDamage(actualDamage);
            RecordIncomingThreat(context, target, status.Source);
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

        private static void PublishStatusRemovedEvent(BattleContext context, RuntimeHero target, RuntimeStatusEffect status)
        {
            if (context?.EventBus == null || target == null || status == null)
            {
                return;
            }

            context.EventBus.Publish(new StatusRemovedEvent(status.Source, target, status.EffectType, status.SourceSkill));
        }

        private static void RecordIncomingThreat(BattleContext context, RuntimeHero target, RuntimeHero source)
        {
            if (context?.Clock == null || target == null || source == null)
            {
                return;
            }

            target.RecordThreat(source, context.Clock.ElapsedTimeSeconds);
        }
    }
}
