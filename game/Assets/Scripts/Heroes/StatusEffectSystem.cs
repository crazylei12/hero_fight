using System;
using Fight.Data;
using UnityEngine;

namespace Fight.Heroes
{
    public static class StatusEffectSystem
    {
        public static bool HasHardControl(RuntimeHero hero)
        {
            return HasBehaviorFlag(hero, StatusBehaviorFlags.BlocksMovement)
                || HasBehaviorFlag(hero, StatusBehaviorFlags.BlocksBasicAttacks)
                || HasBehaviorFlag(hero, StatusBehaviorFlags.BlocksSkillCasts);
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
                if ((statuses[i].Definition.BehaviorFlags & flag) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static float GetModifiedStat(RuntimeHero hero, float baseValue, StatusEffectType effectType)
        {
            return baseValue * GetMultiplier(hero, effectType);
        }

        public static float GetMultiplier(RuntimeHero hero, StatusEffectType effectType)
        {
            if (hero == null)
            {
                return 1f;
            }

            var multiplier = 1f;
            var statuses = hero.MutableStatusEffects;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (statuses[i].EffectType != effectType)
                {
                    continue;
                }

                multiplier += statuses[i].Magnitude;
            }

            return Mathf.Max(0.1f, multiplier);
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

        public static bool TryApplyStatus(RuntimeHero hero, StatusEffectData data, RuntimeHero source = null, SkillData sourceSkill = null)
        {
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
                    refreshTarget.Refresh(data, ShouldRefreshMagnitudeOnReapply(data.effectType));
                    return true;
                }

                return false;
            }

            statuses.Add(new RuntimeStatusEffect(data, source, sourceSkill));
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

                status.Tick(deltaTime);

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

            if (!UsesSourceScopedStackGroups(definition))
            {
                return true;
            }

            return status.Source == source
                && status.SourceSkill == sourceSkill
                && Approximately(status.Magnitude, data.magnitude)
                && Approximately(status.ActiveSkillCooldownCapSeconds, Mathf.Max(0f, data.activeSkillCooldownCapSeconds))
                && Approximately(status.TickIntervalSeconds, Mathf.Max(0.1f, data.tickIntervalSeconds));
        }

        private static bool UsesSourceScopedStackGroups(StatusEffectDefinition definition)
        {
            return definition.IsStatModifier || definition.IsPeriodic;
        }

        private static bool ShouldRefreshMagnitudeOnReapply(StatusEffectType effectType)
        {
            return effectType == StatusEffectType.Shield;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.0001f;
        }
    }
}
