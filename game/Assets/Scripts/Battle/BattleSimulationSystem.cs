using Fight.Data;
using Fight.Core;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleSimulationSystem
    {
        private const int SeparationResolutionPassCount = 4;
        private const float SeparationSqrEpsilon = 0.0001f;

        public static void Tick(BattleContext context, float deltaTime, IBattleSimulationCallbacks battleCallbacks)
        {
            if (context == null || battleCallbacks == null)
            {
                return;
            }

            BattleBasicAttackSystem.TickProjectiles(context, deltaTime, battleCallbacks);
            TickSkillAreas(context, deltaTime, battleCallbacks);
            BattleDeployableProxySystem.Tick(context, deltaTime, battleCallbacks);

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                TickHero(context, context.Heroes[i], deltaTime, battleCallbacks);
            }

            BattleReactiveGuardSystem.Tick(context, deltaTime);
            BattleSkillSystem.TickDelayedSkillEffects(context, deltaTime, battleCallbacks);
            BattleSkillSystem.TickReturningPathStrikes(context, deltaTime, battleCallbacks);
            BattleSkillSystem.TickRadialSweeps(context, deltaTime, battleCallbacks);
            ResolveHeroMinimumSeparation(context);
        }

        private static void TickSkillAreas(BattleContext context, float deltaTime, IBattleSimulationCallbacks battleCallbacks)
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
                    BattleSkillSystem.ResolveSkillAreaPulse(context, area, battleCallbacks);
                }

                if (area.IsExpired)
                {
                    context.SkillAreas.RemoveAt(i);
                }
            }
        }

        private static void TickHero(BattleContext context, RuntimeHero hero, float deltaTime, IBattleSimulationCallbacks battleCallbacks)
        {
            hero.SetBattleTimeSeconds(context?.Clock != null ? context.Clock.ElapsedTimeSeconds : 0f);
            var previousPassiveAttackPowerBonus = QuantizeModifierValue(hero.PassiveAttackPowerBonusMultiplier);
            var previousPassiveDefenseBonus = QuantizeModifierValue(hero.PassiveDefenseBonusMultiplier);
            var previousPassiveLifestealRatio = QuantizeModifierValue(hero.PassiveLifestealRatio);
            var previousTemporarySkill = hero.CurrentTemporaryOverrideSourceSkill;
            var previousTemporaryLifestealRatio = QuantizeModifierValue(hero.CurrentTemporaryOverrideLifestealRatio);
            var previousVisualScaleMultiplier = QuantizeModifierValue(hero.CurrentVisualScaleMultiplier);
            var previousVisualTintStrength = QuantizeModifierValue(hero.CurrentVisualTintStrength);

            hero.Tick(
                deltaTime,
                status => ResolvePeriodicStatusTick(context, hero, status, battleCallbacks),
                status => PublishStatusRemovedEvent(context, hero, status));

            PublishSkillModifierEvents(
                context,
                hero,
                previousPassiveAttackPowerBonus,
                previousPassiveDefenseBonus,
                previousPassiveLifestealRatio,
                previousTemporarySkill,
                previousTemporaryLifestealRatio,
                previousVisualScaleMultiplier,
                previousVisualTintStrength);

            if (hero.IsDead)
            {
                if (hero.ReadyToRevive())
                {
                    hero.ResetToSpawn();
                    context.EventBus.Publish(new UnitRevivedEvent(hero));
                }

                return;
            }

            if (hero.TryConsumeReadyCombatAction(out var pendingAction))
            {
                ResolvePendingCombatAction(context, hero, pendingAction, battleCallbacks);
                return;
            }

            if (hero.IsUnderForcedMovement || hero.IsActionLocked)
            {
                return;
            }

            if (BattleCombatActionSequenceSystem.TryProgressSequence(context, hero, battleCallbacks))
            {
                return;
            }

            if (context.Input.enableSkills && hero.CanCastSkills && BattleSkillSystem.TryCastSkill(context, hero, battleCallbacks))
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

            if (!BattleBasicAttackSystem.TryResolveHeroAttack(
                    context,
                    hero,
                    hero.AttackRange,
                    out var resolvedTarget,
                    out var resolvedAttack))
            {
                return;
            }

            if (resolvedTarget != hero.CurrentTarget)
            {
                hero.SetTarget(resolvedTarget);
                context.EventBus.Publish(new TargetChangedEvent(hero, resolvedTarget));
            }

            BattleBasicAttackSystem.BeginAttack(context, hero, resolvedTarget, resolvedAttack, battleCallbacks);
        }

        private static void ResolvePendingCombatAction(BattleContext context, RuntimeHero hero, PendingCombatAction pendingAction, IBattleSimulationCallbacks battleCallbacks)
        {
            if (context == null || hero == null || pendingAction == null || battleCallbacks == null)
            {
                return;
            }

            switch (pendingAction.ActionType)
            {
                case CombatActionType.BasicAttack:
                    BattleBasicAttackSystem.ResolvePendingAttack(context, hero, pendingAction, battleCallbacks);
                    break;
                case CombatActionType.SkillCast:
                    BattleSkillSystem.ResolvePendingSkillCast(context, hero, pendingAction, battleCallbacks);
                    break;
            }
        }

        private static bool TryRetreatFromRecentThreat(BattleContext context, RuntimeHero hero, RuntimeHero currentTarget, float deltaTime)
        {
            if (context?.Clock == null || hero == null || currentTarget == null || !hero.CanMove)
            {
                hero?.StopThreatRetreat();
                return false;
            }

            if (hero.IsTaunted)
            {
                hero.StopThreatRetreat();
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
            return BattleBasicAttackSystem.SelectPreferredTargetPreview(context, hero);
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

        private static void ResolveHeroMinimumSeparation(BattleContext context)
        {
            if (context?.Heroes == null || context.Heroes.Count <= 1)
            {
                return;
            }

            var minimumDistance = Stage01ArenaSpec.UnitMinimumSeparationWorldUnits;
            if (minimumDistance <= 0f)
            {
                return;
            }

            var minimumDistanceSqr = minimumDistance * minimumDistance;

            for (var pass = 0; pass < SeparationResolutionPassCount; pass++)
            {
                var adjustedAny = false;

                for (var i = 0; i < context.Heroes.Count - 1; i++)
                {
                    var first = context.Heroes[i];
                    if (!ShouldResolveSeparation(first))
                    {
                        continue;
                    }

                    for (var j = i + 1; j < context.Heroes.Count; j++)
                    {
                        var second = context.Heroes[j];
                        if (!ShouldResolveSeparation(second))
                        {
                            continue;
                        }

                        var offset = second.CurrentPosition - first.CurrentPosition;
                        offset.y = 0f;
                        var currentDistanceSqr = offset.sqrMagnitude;
                        if (currentDistanceSqr >= minimumDistanceSqr - SeparationSqrEpsilon)
                        {
                            continue;
                        }

                        var currentDistance = Mathf.Sqrt(Mathf.Max(0f, currentDistanceSqr));
                        var pushDirection = currentDistance > Mathf.Sqrt(SeparationSqrEpsilon)
                            ? offset / currentDistance
                            : GetSeparationFallbackDirection(first, second);
                        var overlap = minimumDistance - currentDistance;
                        if (overlap <= 0f)
                        {
                            continue;
                        }

                        var adjustment = pushDirection * (overlap * 0.5f);
                        first.CurrentPosition = ClampGroundPosition(first.CurrentPosition - adjustment);
                        second.CurrentPosition = ClampGroundPosition(second.CurrentPosition + adjustment);
                        adjustedAny = true;
                    }
                }

                if (!adjustedAny)
                {
                    return;
                }
            }
        }

        private static bool ShouldResolveSeparation(RuntimeHero hero)
        {
            return hero != null && !hero.IsDead;
        }

        private static Vector3 GetSeparationFallbackDirection(RuntimeHero first, RuntimeHero second)
        {
            if (first != null && second != null && first.Side != second.Side)
            {
                return first.Side == TeamSide.Blue ? Vector3.right : Vector3.left;
            }

            if (first != null && second != null && first.SlotIndex != second.SlotIndex)
            {
                return first.SlotIndex < second.SlotIndex ? Vector3.forward : Vector3.back;
            }

            return Vector3.right;
        }

        private static Vector3 ClampGroundPosition(Vector3 position)
        {
            position = Stage01ArenaSpec.ClampPosition(position);
            position.y = 0f;
            return position;
        }

        private static void ResolvePeriodicStatusTick(BattleContext context, RuntimeHero target, RuntimeStatusEffect status, IBattleSimulationCallbacks battleCallbacks)
        {
            if (context == null || target == null || status == null || battleCallbacks == null)
            {
                return;
            }

            switch (status.EffectType)
            {
                case StatusEffectType.HealOverTime:
                    ResolveHealOverTime(context, target, status);
                    break;
                case StatusEffectType.DamageOverTime:
                    ResolveDamageOverTime(context, target, status, battleCallbacks);
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

            BattleStatsSystem.RecordHealingContribution(context, status.Source, target, actualHeal);
            context.EventBus.Publish(new HealAppliedEvent(status.Source ?? target, target, actualHeal, status.SourceSkill, target.CurrentHealth));
        }

        private static void ResolveDamageOverTime(BattleContext context, RuntimeHero target, RuntimeStatusEffect status, IBattleSimulationCallbacks battleCallbacks)
        {
            var resolvedDamage = DamageResolver.ResolveRawDamage(status.Magnitude, target.Defense);
            var actualDamage = BattleDamageSystem.ApplyResolvedDamage(
                context,
                battleCallbacks,
                status.Source,
                target,
                resolvedDamage,
                DamageSourceKind.StatusEffect,
                status.SourceSkill);
            if (actualDamage <= 0f)
            {
                return;
            }
        }

        private static void PublishStatusRemovedEvent(BattleContext context, RuntimeHero target, RuntimeStatusEffect status)
        {
            if (context?.EventBus == null || target == null || status == null)
            {
                return;
            }

            context.EventBus.Publish(new StatusRemovedEvent(
                status.Source,
                target,
                status.EffectType,
                status.SourceSkill,
                status.AppliedBy));
        }

        private static void PublishSkillModifierEvents(
            BattleContext context,
            RuntimeHero hero,
            float previousPassiveAttackPowerBonus,
            float previousPassiveDefenseBonus,
            float previousPassiveLifestealRatio,
            SkillData previousTemporarySkill,
            float previousTemporaryLifestealRatio,
            float previousVisualScaleMultiplier,
            float previousVisualTintStrength)
        {
            if (context?.EventBus == null || hero == null)
            {
                return;
            }

            var passiveSkill = hero.Definition?.activeSkill;
            var currentPassiveAttackPowerBonus = QuantizeModifierValue(hero.PassiveAttackPowerBonusMultiplier);
            var currentPassiveDefenseBonus = QuantizeModifierValue(hero.PassiveDefenseBonusMultiplier);
            var currentPassiveLifestealRatio = QuantizeModifierValue(hero.PassiveLifestealRatio);
            if (passiveSkill != null
                && passiveSkill.activationMode == SkillActivationMode.Passive
                && !Mathf.Approximately(previousPassiveAttackPowerBonus, currentPassiveAttackPowerBonus))
            {
                context.EventBus.Publish(new PassiveSkillValueChangedEvent(
                    hero,
                    passiveSkill,
                    PassiveSkillValueType.AttackPower,
                    currentPassiveAttackPowerBonus));
            }

            if (passiveSkill != null
                && passiveSkill.activationMode == SkillActivationMode.Passive
                && !Mathf.Approximately(previousPassiveDefenseBonus, currentPassiveDefenseBonus))
            {
                context.EventBus.Publish(new PassiveSkillValueChangedEvent(
                    hero,
                    passiveSkill,
                    PassiveSkillValueType.Defense,
                    currentPassiveDefenseBonus));
            }

            if (passiveSkill != null
                && passiveSkill.activationMode == SkillActivationMode.Passive
                && !Mathf.Approximately(previousPassiveLifestealRatio, currentPassiveLifestealRatio))
            {
                context.EventBus.Publish(new PassiveSkillValueChangedEvent(
                    hero,
                    passiveSkill,
                    PassiveSkillValueType.Lifesteal,
                    currentPassiveLifestealRatio));
            }

            var currentTemporarySkill = hero.CurrentTemporaryOverrideSourceSkill;
            var currentTemporaryLifestealRatio = QuantizeModifierValue(hero.CurrentTemporaryOverrideLifestealRatio);
            var currentVisualScaleMultiplier = QuantizeModifierValue(hero.CurrentVisualScaleMultiplier);
            var currentVisualTintStrength = QuantizeModifierValue(hero.CurrentVisualTintStrength);
            var temporaryStateChanged =
                previousTemporarySkill != currentTemporarySkill
                || !Mathf.Approximately(previousTemporaryLifestealRatio, currentTemporaryLifestealRatio)
                || !Mathf.Approximately(previousVisualScaleMultiplier, currentVisualScaleMultiplier)
                || !Mathf.Approximately(previousVisualTintStrength, currentVisualTintStrength);

            if (!temporaryStateChanged)
            {
                return;
            }

            if (previousTemporarySkill != null
                && (currentTemporarySkill != previousTemporarySkill
                    || currentTemporaryLifestealRatio <= Mathf.Epsilon
                    && currentVisualScaleMultiplier <= 1f + Mathf.Epsilon
                    && currentVisualTintStrength <= Mathf.Epsilon))
            {
                context.EventBus.Publish(new SkillTemporaryOverrideChangedEvent(
                    hero,
                    previousTemporarySkill,
                    false,
                    0f,
                    1f,
                    0f));
            }

            if (currentTemporarySkill != null
                && (currentTemporaryLifestealRatio > Mathf.Epsilon
                    || currentVisualScaleMultiplier > 1f + Mathf.Epsilon
                    || currentVisualTintStrength > Mathf.Epsilon))
            {
                context.EventBus.Publish(new SkillTemporaryOverrideChangedEvent(
                    hero,
                    currentTemporarySkill,
                    true,
                    currentTemporaryLifestealRatio,
                    currentVisualScaleMultiplier,
                    currentVisualTintStrength));
            }
        }

        private static float QuantizeModifierValue(float value)
        {
            return Mathf.Round(value * 100f) * 0.01f;
        }
    }
}
