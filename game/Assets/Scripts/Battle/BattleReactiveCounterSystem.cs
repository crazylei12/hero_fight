using System.Collections.Generic;
using Fight.Core;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleReactiveCounterSystem
    {
        public static void ApplyReactiveCounter(RuntimeHero hero, SkillData sourceSkill)
        {
            if (hero == null
                || hero.IsDead
                || sourceSkill?.reactiveCounter == null
                || !sourceSkill.reactiveCounter.enabled)
            {
                return;
            }

            hero.ApplyReactiveCounter(sourceSkill, sourceSkill.reactiveCounter);
        }

        public static void TryProcessDamage(
            BattleContext context,
            IBattleSimulationCallbacks battleCallbacks,
            RuntimeHero attacker,
            RuntimeHero defender,
            DamageSourceKind sourceKind,
            RuntimeDeployableProxy sourceProxy,
            float effectiveDamageAmount)
        {
            if (context == null
                || battleCallbacks == null
                || attacker == null
                || defender == null
                || attacker == defender
                || attacker.IsDead
                || defender.IsDead
                || attacker.Side == defender.Side
                || effectiveDamageAmount <= Mathf.Epsilon
                || !defender.HasActiveReactiveCounter)
            {
                return;
            }

            var counterData = defender.ActiveReactiveCounterData;
            var sourceSkill = defender.ActiveReactiveCounterSourceSkill;
            if (counterData == null
                || sourceSkill == null
                || !counterData.enabled
                || sourceProxy != null
                || (counterData.triggerOnBasicAttackDamage && sourceKind != DamageSourceKind.BasicAttack)
                || (counterData.requireNonProjectileBasicAttack && attacker.UsesProjectileBasicAttack))
            {
                return;
            }

            var currentTimeSeconds = context.Clock != null ? context.Clock.ElapsedTimeSeconds : defender.CurrentBattleTimeSeconds;
            if (!defender.TryConsumeReactiveCounterTrigger(attacker, currentTimeSeconds))
            {
                return;
            }

            var actualCounterDamage = ApplyCounterDamage(context, battleCallbacks, defender, attacker, sourceSkill, counterData);
            if (!attacker.IsDead && attacker.CurrentHealth > 0f)
            {
                ApplyCounterStatuses(context, defender, attacker, sourceSkill, counterData);
                ApplyCounterForcedMovement(context, defender, attacker, sourceSkill, counterData);
            }

            context.EventBus?.Publish(new ReactiveCounterTriggeredEvent(
                defender,
                attacker,
                sourceSkill,
                actualCounterDamage));
        }

        private static float ApplyCounterDamage(
            BattleContext context,
            IBattleSimulationCallbacks battleCallbacks,
            RuntimeHero defender,
            RuntimeHero attacker,
            SkillData sourceSkill,
            ReactiveCounterData counterData)
        {
            var powerMultiplier = Mathf.Max(0f, counterData.counterDamagePowerMultiplier);
            if (powerMultiplier <= Mathf.Epsilon)
            {
                return 0f;
            }

            var damage = DamageResolver.ResolveDamage(
                defender.AttackPower,
                defender.CriticalChance,
                defender.CriticalDamageMultiplier,
                attacker.Defense,
                context.RandomService,
                powerMultiplier);
            if (damage <= Mathf.Epsilon)
            {
                return 0f;
            }

            return BattleDamageSystem.ApplyResolvedDamage(
                context,
                battleCallbacks,
                defender,
                attacker,
                damage,
                DamageSourceKind.CounterTrigger,
                sourceSkill);
        }

        private static void ApplyCounterStatuses(
            BattleContext context,
            RuntimeHero defender,
            RuntimeHero attacker,
            SkillData sourceSkill,
            ReactiveCounterData counterData)
        {
            if (counterData.onTriggerStatusEffects == null)
            {
                return;
            }

            for (var i = 0; i < counterData.onTriggerStatusEffects.Count; i++)
            {
                var status = counterData.onTriggerStatusEffects[i];
                if (status == null || !attacker.ApplyStatusEffect(status, defender, sourceSkill, defender, out var appliedStatus))
                {
                    continue;
                }

                var appliedSource = appliedStatus?.Source ?? defender;
                BattleStatsSystem.RecordStatusContribution(context, appliedSource, attacker, status);
                context.EventBus?.Publish(new StatusAppliedEvent(
                    appliedSource,
                    attacker,
                    status.effectType,
                    status.durationSeconds,
                    appliedStatus?.Magnitude ?? status.magnitude,
                    sourceSkill,
                    appliedStatus?.AppliedBy ?? defender));
            }
        }

        private static void ApplyCounterForcedMovement(
            BattleContext context,
            RuntimeHero defender,
            RuntimeHero attacker,
            SkillData sourceSkill,
            ReactiveCounterData counterData)
        {
            if (counterData.forcedMovementDistance <= Mathf.Epsilon
                && counterData.forcedMovementDurationSeconds <= Mathf.Epsilon)
            {
                return;
            }

            var effect = new SkillEffectData
            {
                forcedMovementDirection = ForcedMovementDirectionMode.AwayFromSource,
                forcedMovementDistance = counterData.forcedMovementDistance,
                forcedMovementDurationSeconds = counterData.forcedMovementDurationSeconds,
                forcedMovementPeakHeight = counterData.forcedMovementPeakHeight,
            };
            BattleForcedMovementUtility.ApplyForcedMovementToTargets(
                context,
                defender,
                defender.CurrentPosition,
                sourceSkill,
                effect,
                new List<RuntimeHero> { attacker });
        }
    }
}
