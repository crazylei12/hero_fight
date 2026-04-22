using System;
using System.Collections.Generic;
using Fight.Data;
using UnityEngine;

namespace Fight.Heroes
{
    public readonly struct DamageShareTransfer
    {
        public DamageShareTransfer(RuntimeHero receiver, float damageAmount)
        {
            Receiver = receiver;
            DamageAmount = damageAmount;
        }

        public RuntimeHero Receiver { get; }

        public float DamageAmount { get; }
    }

    public static class StatusEffectSystem
    {
        public static bool HasHardControl(RuntimeHero hero)
        {
            return HasBehaviorFlag(hero, StatusBehaviorFlags.HardControl);
        }

        public static bool HasBehaviorFlag(RuntimeHero hero, StatusBehaviorFlags flag)
        {
            if (hero == null)
            {
                return false;
            }

            var statuses = hero.MutableStatusEffects;
            for (var i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status == null || !IsBehaviorActive(hero, status))
                {
                    continue;
                }

                if ((status.Definition.BehaviorFlags & flag) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetForcedEnemyTarget(RuntimeHero hero, out RuntimeHero forcedTarget)
        {
            forcedTarget = null;
            if (hero == null)
            {
                return false;
            }

            var statuses = hero.MutableStatusEffects;
            for (var i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status == null
                    || !status.Definition.ForcesEnemyTarget
                    || !IsValidForcedTargetSource(hero, status))
                {
                    continue;
                }

                forcedTarget = status.Source;
                return true;
            }

            return false;
        }

        public static float GetModifiedStat(RuntimeHero hero, float baseValue, StatusEffectType effectType)
        {
            return baseValue * GetMultiplier(hero, effectType);
        }

        public static float GetTotalMagnitude(RuntimeHero hero, StatusEffectType effectType)
        {
            if (hero == null)
            {
                return 0f;
            }

            var magnitude = 0f;
            var statuses = hero.MutableStatusEffects;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (statuses[i].EffectType == effectType)
                {
                    magnitude += statuses[i].Magnitude;
                }
            }

            return magnitude;
        }

        public static float GetMultiplier(RuntimeHero hero, StatusEffectType effectType)
        {
            return Mathf.Max(0.1f, 1f + GetTotalMagnitude(hero, effectType));
        }

        public static float GetHealTakenMultiplier(RuntimeHero hero)
        {
            return Mathf.Max(0f, 1f + GetTotalMagnitude(hero, StatusEffectType.HealTakenModifier));
        }

        public static bool HasStatus(RuntimeHero hero, StatusEffectType effectType)
        {
            if (hero == null)
            {
                return false;
            }

            var statuses = hero.MutableStatusEffects;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (statuses[i].EffectType == effectType)
                {
                    return true;
                }
            }

            return false;
        }

        public static float GetActiveSkillCooldownCap(RuntimeHero hero)
        {
            if (hero == null)
            {
                return 0f;
            }

            var bestCap = 0f;
            var statuses = hero.MutableStatusEffects;
            for (var i = 0; i < statuses.Count; i++)
            {
                var cap = statuses[i].ActiveSkillCooldownCapSeconds;
                if (cap <= 0f)
                {
                    continue;
                }

                bestCap = bestCap <= 0f ? cap : Mathf.Min(bestCap, cap);
            }

            return bestCap;
        }

        public static float GetTotalShield(RuntimeHero hero)
        {
            if (hero == null)
            {
                return 0f;
            }

            var shieldAmount = 0f;
            var statuses = hero.MutableStatusEffects;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (statuses[i].EffectType == StatusEffectType.Shield)
                {
                    shieldAmount += Mathf.Max(0f, statuses[i].Magnitude);
                }
            }

            return shieldAmount;
        }

        public static void GetDamageShareTransfers(RuntimeHero hero, float damageAmount, List<DamageShareTransfer> results)
        {
            results?.Clear();
            if (hero == null || results == null || damageAmount <= 0f)
            {
                return;
            }

            var statuses = hero.MutableStatusEffects;
            var totalRequestedShareRatio = 0f;
            List<RuntimeHero> candidateReceivers = null;
            List<float> candidateRatios = null;

            for (var i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status == null || status.EffectType != StatusEffectType.DamageShare)
                {
                    continue;
                }

                var receiver = status.Source;
                if (!IsValidDamageShareReceiver(hero, receiver))
                {
                    continue;
                }

                var shareRatio = Mathf.Max(0f, status.Magnitude);
                if (shareRatio <= Mathf.Epsilon)
                {
                    continue;
                }

                candidateReceivers ??= new List<RuntimeHero>();
                candidateRatios ??= new List<float>();
                candidateReceivers.Add(receiver);
                candidateRatios.Add(shareRatio);
                totalRequestedShareRatio += shareRatio;
            }

            if (candidateReceivers == null || totalRequestedShareRatio <= Mathf.Epsilon)
            {
                return;
            }

            var totalSharedDamage = damageAmount * Mathf.Clamp01(totalRequestedShareRatio);
            for (var i = 0; i < candidateReceivers.Count; i++)
            {
                var receiver = candidateReceivers[i];
                var shareDamage = totalSharedDamage * (candidateRatios[i] / totalRequestedShareRatio);
                if (shareDamage <= Mathf.Epsilon)
                {
                    continue;
                }

                var merged = false;
                for (var existingIndex = 0; existingIndex < results.Count; existingIndex++)
                {
                    if (results[existingIndex].Receiver != receiver)
                    {
                        continue;
                    }

                    results[existingIndex] = new DamageShareTransfer(
                        receiver,
                        results[existingIndex].DamageAmount + shareDamage);
                    merged = true;
                    break;
                }

                if (!merged)
                {
                    results.Add(new DamageShareTransfer(receiver, shareDamage));
                }
            }
        }

        public static bool TryApplyStatus(
            RuntimeHero hero,
            StatusEffectData data,
            RuntimeHero source,
            SkillData sourceSkill,
            RuntimeHero appliedBy,
            out RuntimeStatusEffect appliedStatus)
        {
            appliedStatus = null;
            if (hero == null || data == null || data.effectType == StatusEffectType.None)
            {
                return false;
            }

            var statuses = hero.MutableStatusEffects;
            var definition = StatusEffectCatalog.Get(data.effectType);
            var sameTypeCount = 0;
            RuntimeStatusEffect refreshTarget = null;

            for (var i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (!BelongsToSameStackGroup(status, data, source, sourceSkill, definition))
                {
                    continue;
                }

                sameTypeCount++;
                if (refreshTarget == null || status.RemainingDurationSeconds < refreshTarget.RemainingDurationSeconds - Mathf.Epsilon)
                {
                    refreshTarget = status;
                }
            }

            if (sameTypeCount >= Mathf.Max(1, data.maxStacks))
            {
                if (data.refreshDurationOnReapply && refreshTarget != null)
                {
                    refreshTarget.Refresh(data, hero, source, sourceSkill, appliedBy, ShouldRefreshMagnitudeOnReapply(data.effectType));
                    appliedStatus = refreshTarget;
                    return true;
                }

                return false;
            }

            appliedStatus = new RuntimeStatusEffect(data, hero, source, sourceSkill, appliedBy);
            statuses.Add(appliedStatus);
            return true;
        }

        public static void Tick(
            RuntimeHero hero,
            float deltaTime,
            Action<RuntimeStatusEffect> onPeriodicStatusTick = null,
            Action<RuntimeStatusEffect> onExpiredStatus = null)
        {
            if (hero == null)
            {
                return;
            }

            var statuses = hero.MutableStatusEffects;
            var statusSnapshot = statuses.ToArray();
            for (var i = statusSnapshot.Length - 1; i >= 0; i--)
            {
                var status = statusSnapshot[i];
                if (status == null || !statuses.Contains(status))
                {
                    continue;
                }

                if (ShouldExpireFromSourceLoss(hero, status))
                {
                    status.ExpireImmediately();
                }
                else
                {
                    status.Tick(deltaTime);
                }

                var pendingTickCount = status.ConsumePendingTickCount();
                for (var tickIndex = 0; tickIndex < pendingTickCount; tickIndex++)
                {
                    onPeriodicStatusTick?.Invoke(status);
                    if (hero.IsDead)
                    {
                        break;
                    }
                }

                if (hero.IsDead)
                {
                    break;
                }

                if (!status.IsExpired)
                {
                    continue;
                }

                onExpiredStatus?.Invoke(status);
                statuses.Remove(status);
            }
        }

        public static float ConsumeShield(RuntimeHero hero, float damageAmount)
        {
            if (hero == null || damageAmount <= 0f)
            {
                return 0f;
            }

            var remainingDamage = Mathf.Max(0f, damageAmount);
            var absorbedDamage = 0f;
            var statuses = hero.MutableStatusEffects;

            for (var i = 0; i < statuses.Count && remainingDamage > 0f; i++)
            {
                var status = statuses[i];
                if (status.EffectType != StatusEffectType.Shield)
                {
                    continue;
                }

                var absorbed = status.ConsumeMagnitude(remainingDamage);
                absorbedDamage += absorbed;
                remainingDamage -= absorbed;
            }

            return absorbedDamage;
        }

        public static void RemoveExpiredStatuses(RuntimeHero hero, Action<RuntimeStatusEffect> onExpiredStatus = null)
        {
            if (hero == null)
            {
                return;
            }

            var statuses = hero.MutableStatusEffects;
            for (var i = statuses.Count - 1; i >= 0; i--)
            {
                var status = statuses[i];
                if (!status.IsExpired)
                {
                    continue;
                }

                onExpiredStatus?.Invoke(status);
                statuses.RemoveAt(i);
            }
        }

        public static void ClearStatuses(RuntimeHero hero, Action<RuntimeStatusEffect> onRemovedStatus = null)
        {
            if (hero == null)
            {
                return;
            }

            var statuses = hero.MutableStatusEffects;
            for (var i = statuses.Count - 1; i >= 0; i--)
            {
                var status = statuses[i];
                onRemovedStatus?.Invoke(status);
                statuses.RemoveAt(i);
            }
        }

        private static bool BelongsToSameStackGroup(
            RuntimeStatusEffect status,
            StatusEffectData data,
            RuntimeHero source,
            SkillData sourceSkill,
            StatusEffectDefinition definition)
        {
            if (status == null || status.EffectType != data.effectType)
            {
                return false;
            }

            var stackGroupKey = data.stackGroupKey ?? string.Empty;
            if (definition.EffectType == StatusEffectType.DamageShare)
            {
                return status.Source == source
                    && status.SourceSkill == sourceSkill;
            }

            if (!string.IsNullOrWhiteSpace(status.StackGroupKey) || !string.IsNullOrWhiteSpace(stackGroupKey))
            {
                return !string.IsNullOrWhiteSpace(stackGroupKey)
                    && status.Source == source
                    && string.Equals(status.StackGroupKey, stackGroupKey, StringComparison.Ordinal);
            }

            if (!UsesSourceScopedStackGroups(definition))
            {
                return true;
            }

            return status.Source == source
                && status.SourceSkill == sourceSkill
                && Approximately(status.BaseMagnitude, data.magnitude)
                && Approximately(status.SourceAttackPowerMultiplier, Mathf.Max(0f, data.sourceAttackPowerMultiplier))
                && Approximately(status.ActiveSkillCooldownCapSeconds, Mathf.Max(0f, data.activeSkillCooldownCapSeconds))
                && Approximately(status.TickIntervalSeconds, Mathf.Max(0.1f, data.tickIntervalSeconds));
        }

        private static bool MatchesStatusQuery(RuntimeStatusEffect status, StatusEffectType effectType, string statusThemeKey)
        {
            if (status == null || status.EffectType != effectType)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(statusThemeKey))
            {
                return true;
            }

            return string.Equals(status.StatusThemeKey, statusThemeKey, StringComparison.Ordinal);
        }

        private static bool UsesSourceScopedStackGroups(StatusEffectDefinition definition)
        {
            return definition.IsStatModifier
                || definition.IsPeriodic
                || definition.EffectType == StatusEffectType.DamageShare;
        }

        private static bool IsBehaviorActive(RuntimeHero hero, RuntimeStatusEffect status)
        {
            if (hero == null || status == null)
            {
                return false;
            }

            if (status.Definition.ForcesEnemyTarget)
            {
                return IsValidForcedTargetSource(hero, status);
            }

            return true;
        }

        private static bool ShouldExpireFromSourceLoss(RuntimeHero hero, RuntimeStatusEffect status)
        {
            if (hero == null || status == null)
            {
                return false;
            }

            if (status.Definition.ForcesEnemyTarget)
            {
                return !IsValidForcedTargetSource(hero, status);
            }

            if (status.EffectType == StatusEffectType.DamageShare)
            {
                return !IsValidDamageShareReceiver(hero, status.Source);
            }

            return false;
        }

        public static int GetStatusStackCount(RuntimeHero hero, StatusEffectType effectType, string statusThemeKey = null)
        {
            if (hero == null || effectType == StatusEffectType.None)
            {
                return 0;
            }

            var count = 0;
            var statuses = hero.MutableStatusEffects;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (MatchesStatusQuery(statuses[i], effectType, statusThemeKey))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsValidForcedTargetSource(RuntimeHero hero, RuntimeStatusEffect status)
        {
            if (hero == null || status?.Source == null)
            {
                return false;
            }

            return !status.Source.IsDead
                && status.Source.Side != hero.Side
                && status.Source.CanBeDirectTargeted;
        }

        private static bool ShouldRefreshMagnitudeOnReapply(StatusEffectType effectType)
        {
            return effectType == StatusEffectType.Shield
                || effectType == StatusEffectType.DamageShare;
        }

        private static bool IsValidDamageShareReceiver(RuntimeHero protectedHero, RuntimeHero receiver)
        {
            return protectedHero != null
                && receiver != null
                && receiver != protectedHero
                && !receiver.IsDead
                && receiver.Side == protectedHero.Side;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.0001f;
        }
    }
}
