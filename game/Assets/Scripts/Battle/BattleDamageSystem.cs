using System;
using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleDamageSystem
    {
        public static float ApplyResolvedDamage(
            BattleContext context,
            BattleManager battleManager,
            RuntimeHero attacker,
            RuntimeHero target,
            float damageAmount,
            DamageSourceKind sourceKind,
            SkillData sourceSkill = null)
        {
            var actualDamage = ApplyResolvedDamageInternal(
                context,
                battleManager,
                attacker,
                target,
                damageAmount,
                sourceKind,
                sourceSkill,
                allowDamageShare: true);
            TryApplyLifesteal(context, attacker, actualDamage);
            return actualDamage;
        }

        private static float ApplyResolvedDamageInternal(
            BattleContext context,
            BattleManager battleManager,
            RuntimeHero attacker,
            RuntimeHero target,
            float damageAmount,
            DamageSourceKind sourceKind,
            SkillData sourceSkill,
            bool allowDamageShare)
        {
            if (context == null || battleManager == null || target == null || damageAmount <= 0f || target.IsDead || !target.CanReceiveDamage)
            {
                return 0f;
            }

            Action<RuntimeStatusEffect> onExpiredStatus = status => PublishStatusRemovedEvent(context, target, status);
            var remainingDamage = Mathf.Max(0f, damageAmount);
            var absorbedByShield = target.ConsumeShield(remainingDamage, onExpiredStatus);
            if (absorbedByShield > Mathf.Epsilon)
            {
                BattleStatsSystem.RecordDamageContribution(context, attacker, target, absorbedByShield);
            }

            if (absorbedByShield > Mathf.Epsilon || remainingDamage > Mathf.Epsilon)
            {
                RecordIncomingThreat(context, target, attacker);
            }

            remainingDamage -= absorbedByShield;
            if (remainingDamage <= Mathf.Epsilon)
            {
                return absorbedByShield;
            }

            var damageToTarget = remainingDamage;
            List<DamageShareTransfer> damageShareTransfers = null;
            if (allowDamageShare)
            {
                damageShareTransfers = new List<DamageShareTransfer>();
                StatusEffectSystem.GetDamageShareTransfers(target, remainingDamage, damageShareTransfers);
                if (damageShareTransfers.Count > 0)
                {
                    var totalSharedDamage = 0f;
                    for (var i = 0; i < damageShareTransfers.Count; i++)
                    {
                        totalSharedDamage += damageShareTransfers[i].DamageAmount;
                    }

                    damageToTarget = Mathf.Max(0f, remainingDamage - totalSharedDamage);
                }
            }

            var totalActualDamage = 0f;
            if (damageToTarget > Mathf.Epsilon)
            {
                totalActualDamage += ApplyHealthDamageToTarget(
                    context,
                    battleManager,
                    attacker,
                    target,
                    damageToTarget,
                    sourceKind,
                    sourceSkill);
            }

            if (damageShareTransfers == null || damageShareTransfers.Count == 0)
            {
                return totalActualDamage;
            }

            for (var i = 0; i < damageShareTransfers.Count; i++)
            {
                var transfer = damageShareTransfers[i];
                totalActualDamage += ApplyResolvedDamageInternal(
                    context,
                    battleManager,
                    attacker,
                    transfer.Receiver,
                    transfer.DamageAmount,
                    DamageSourceKind.DamageShare,
                    sourceSkill,
                    allowDamageShare: false);
            }

            return totalActualDamage;
        }

        private static float ApplyHealthDamageToTarget(
            BattleContext context,
            BattleManager battleManager,
            RuntimeHero attacker,
            RuntimeHero target,
            float damageAmount,
            DamageSourceKind sourceKind,
            SkillData sourceSkill)
        {
            if (context == null || battleManager == null || target == null || damageAmount <= 0f)
            {
                return 0f;
            }

            var actualDamage = target.ApplyHealthLoss(damageAmount);
            if (actualDamage <= 0f)
            {
                return 0f;
            }

            BattleStatsSystem.RecordDamageContribution(context, attacker, target, actualDamage);
            context.EventBus?.Publish(new DamageAppliedEvent(
                attacker,
                target,
                actualDamage,
                sourceKind,
                sourceSkill,
                target.CurrentHealth));

            if (!target.IsDead && target.CurrentHealth > 0f)
            {
                return actualDamage;
            }

            BattleStatsSystem.ResolveAssists(context, target, attacker);
            target.MarkDead(
                context.Input.respawnDelaySeconds,
                status => PublishStatusRemovedEvent(context, target, status));
            attacker?.MarkKill();
            context.EventBus?.Publish(new UnitDiedEvent(target, attacker));

            if (attacker != null)
            {
                battleManager.RegisterKill(attacker.Side);
            }

            return actualDamage;
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

        private static void RecordIncomingThreat(BattleContext context, RuntimeHero target, RuntimeHero source)
        {
            if (context?.Clock == null || target == null || source == null)
            {
                return;
            }

            target.RecordThreat(source, context.Clock.ElapsedTimeSeconds);
        }

        private static void TryApplyLifesteal(BattleContext context, RuntimeHero attacker, float actualDamage)
        {
            if (context == null
                || attacker == null
                || actualDamage <= Mathf.Epsilon)
            {
                return;
            }

            var lifestealRatio = attacker.CurrentLifestealRatio;
            if (lifestealRatio <= Mathf.Epsilon)
            {
                return;
            }

            var healAmount = actualDamage * lifestealRatio;
            var actualHeal = attacker.ApplyHealing(healAmount);
            if (actualHeal <= Mathf.Epsilon)
            {
                return;
            }

            BattleStatsSystem.RecordHealingContribution(context, attacker, attacker, actualHeal);
            context.EventBus?.Publish(new HealAppliedEvent(
                attacker,
                attacker,
                actualHeal,
                attacker.CurrentTemporaryOverrideSourceSkill,
                attacker.CurrentHealth));
        }
    }
}
