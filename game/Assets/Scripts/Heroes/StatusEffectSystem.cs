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

        public static bool TryApplyStatus(RuntimeHero hero, StatusEffectData data, RuntimeHero source = null, SkillData sourceSkill = null)
        {
            if (hero == null || data == null || data.effectType == StatusEffectType.None)
            {
                return false;
            }

            var statuses = hero.MutableStatusEffects;
            var sameTypeCount = 0;
            RuntimeStatusEffect refreshTarget = null;

            for (var i = 0; i < statuses.Count; i++)
            {
                if (statuses[i].EffectType != data.effectType)
                {
                    continue;
                }

                sameTypeCount++;
                refreshTarget ??= statuses[i];
            }

            if (sameTypeCount >= Mathf.Max(1, data.maxStacks))
            {
                if (data.refreshDurationOnReapply && refreshTarget != null)
                {
                    refreshTarget.Refresh(data);
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

        public static void ClearStatuses(RuntimeHero hero)
        {
            hero?.MutableStatusEffects.Clear();
        }
    }
}
