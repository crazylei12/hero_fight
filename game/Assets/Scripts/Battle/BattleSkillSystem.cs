using System.Collections.Generic;
using Fight.Core;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleSkillSystem
    {
        private const float UltimateInitialLockoutSeconds = 6f;
        private const float UltimateDecisionIntervalSeconds = 0.75f;
        private const float UltimateDecisionJitterSeconds = 0.5f;
        private const float UltimateBaseReleaseChance = 0.3f;
        private const float UltimateExtraUnitReleaseChance = 0.15f;
        private const float UltimateFirstFallbackBonus = 0.2f;
        private const float UltimateSecondFallbackBonus = 0.5f;
        private const float UltimateSecondaryPriorityBonus = 0.25f;
        private const float UltimateAllySuppressionWindowSeconds = 5f;
        private const float UltimateAllySuppressionChanceMultiplier = 0.25f;

        public static bool TryCastSkill(BattleContext context, RuntimeHero caster, BattleManager battleManager)
        {
            if (context == null || caster == null || battleManager == null || caster.IsDead)
            {
                return false;
            }

            if (!caster.CanCastSkills)
            {
                return false;
            }

            if (TryCastUltimate(context, caster, caster.Definition?.ultimateSkill, battleManager))
            {
                return true;
            }

            return TryCastActiveSkill(context, caster, caster.Definition?.activeSkill, battleManager);
        }

        private static bool TryCastActiveSkill(BattleContext context, RuntimeHero caster, SkillData skill, BattleManager battleManager)
        {
            return TryCastSpecificSkill(context, caster, skill, battleManager, requireHighValueCast: false);
        }

        private static bool TryCastUltimate(BattleContext context, RuntimeHero caster, SkillData skill, BattleManager battleManager)
        {
            if (skill == null)
            {
                return false;
            }

            if (skill.slotType != SkillSlotType.Ultimate || !caster.CanUseUltimate())
            {
                return false;
            }

            if (!ShouldAttemptUltimateCast(context, caster))
            {
                return false;
            }

            var usesTemplateDecision = UsesUltimateDecisionTemplate(skill);
            var primaryTarget = usesTemplateDecision
                ? SelectUltimatePrimaryTarget(context, caster, skill)
                : SelectPrimaryTarget(context, caster, skill);
            if (!IsPrimaryTargetStillValid(skill, caster, primaryTarget))
            {
                ScheduleNextUltimateAttempt(context, caster);
                return false;
            }

            if (primaryTarget == null && !skill.allowsSelfCast && !AllowsMissingPrimaryTarget(skill))
            {
                ScheduleNextUltimateAttempt(context, caster);
                return false;
            }

            var affectedTargets = CollectTargets(context, caster, skill, primaryTarget);
            if (usesTemplateDecision)
            {
                if (affectedTargets.Count <= 0)
                {
                    ScheduleNextUltimateAttempt(context, caster);
                    return false;
                }
            }
            else if (affectedTargets.Count < Mathf.Max(1, skill.minTargetsToCast))
            {
                ScheduleNextUltimateAttempt(context, caster);
                return false;
            }

            if (usesTemplateDecision)
            {
                if (!EvaluateUltimateDecision(context, caster, skill, primaryTarget, affectedTargets))
                {
                    ScheduleNextUltimateAttempt(context, caster);
                    return false;
                }

                if (!RollUltimateCastChance(context, caster, skill, primaryTarget))
                {
                    return false;
                }
            }
            else if (!HasHighValueOpportunity(skill, affectedTargets))
            {
                ScheduleNextUltimateAttempt(context, caster);
                return false;
            }
            else if (!RollLegacyUltimateCastChance(context, caster, skill, affectedTargets))
            {
                return false;
            }

            BeginSkillCast(context, caster, skill, primaryTarget, affectedTargets, battleManager);
            return true;
        }

        private static bool TryCastSpecificSkill(BattleContext context, RuntimeHero caster, SkillData skill, BattleManager battleManager, bool requireHighValueCast)
        {
            if (skill == null)
            {
                return false;
            }

            if (skill.slotType == SkillSlotType.ActiveSkill && !caster.CanUseActiveSkill())
            {
                return false;
            }

            if (skill.slotType == SkillSlotType.Ultimate && !caster.CanUseUltimate())
            {
                return false;
            }

            var primaryTarget = SelectPrimaryTarget(context, caster, skill);
            if (!IsPrimaryTargetStillValid(skill, caster, primaryTarget))
            {
                return false;
            }

            if (primaryTarget == null && !skill.allowsSelfCast && !AllowsMissingPrimaryTarget(skill))
            {
                return false;
            }

            var affectedTargets = CollectTargets(context, caster, skill, primaryTarget);
            if (affectedTargets.Count < Mathf.Max(1, skill.minTargetsToCast))
            {
                return false;
            }

            if (requireHighValueCast && !HasHighValueOpportunity(skill, affectedTargets))
            {
                return false;
            }

            BeginSkillCast(context, caster, skill, primaryTarget, affectedTargets, battleManager);
            return true;
        }

        private static bool UsesUltimateDecisionTemplate(SkillData skill)
        {
            return skill != null
                && skill.slotType == SkillSlotType.Ultimate
                && skill.ultimateDecision != null
                && skill.ultimateDecision.primaryCondition != null
                && skill.ultimateDecision.primaryCondition.conditionType != UltimateConditionType.None;
        }

        private static bool ShouldAttemptUltimateCast(BattleContext context, RuntimeHero caster)
        {
            if (context?.Clock == null || caster == null)
            {
                return false;
            }

            var elapsedTime = context.Clock.ElapsedTimeSeconds;
            if (!caster.HasInitializedUltimateDecisionSchedule)
            {
                caster.InitializeUltimateDecisionSchedule(UltimateInitialLockoutSeconds + GetRandomJitter(context));
            }

            if (elapsedTime < UltimateInitialLockoutSeconds)
            {
                return false;
            }

            return elapsedTime >= caster.NextUltimateDecisionCheckTimeSeconds;
        }

        private static void ScheduleNextUltimateAttempt(BattleContext context, RuntimeHero caster)
        {
            if (context?.Clock == null || caster == null)
            {
                return;
            }

            var nextCheckTime = context.Clock.ElapsedTimeSeconds + UltimateDecisionIntervalSeconds + GetRandomJitter(context);
            caster.ScheduleNextUltimateDecisionCheck(nextCheckTime);
        }

        private static float GetRandomJitter(BattleContext context)
        {
            return context?.RandomService != null
                ? context.RandomService.NextFloat() * UltimateDecisionJitterSeconds
                : 0f;
        }

        private static RuntimeHero SelectPrimaryTarget(BattleContext context, RuntimeHero caster, SkillData skill)
        {
            var effectiveCastRange = GetSkillSelectionCastRange(skill);
            switch (skill.targetType)
            {
                case SkillTargetType.Self:
                    return caster;
                case SkillTargetType.AllAllies:
                    return FindFirstGlobalTeamTarget(context.Heroes, caster, skill, includeAllies: true);
                case SkillTargetType.AllEnemies:
                    return FindFirstGlobalTeamTarget(context.Heroes, caster, skill, includeAllies: false);
                case SkillTargetType.NearestEnemy:
                    return SelectCurrentOrNearestEnemyTarget(context, caster, skill, effectiveCastRange);
                case SkillTargetType.LowestHealthEnemy:
                    return FindLowestHealth(context.Heroes, caster, skill, includeAllies: false, effectiveCastRange);
                case SkillTargetType.LowestHealthAlly:
                    return FindLowestHealth(context.Heroes, caster, skill, includeAllies: true, effectiveCastRange);
                case SkillTargetType.HighestDamageEnemyInRange:
                    return BattleAiDirector.SelectHighestDamageEnemyTarget(context.Heroes, caster, effectiveCastRange);
                case SkillTargetType.DensestEnemyArea:
                    return FindDensestEnemyAnchor(context.Heroes, caster, effectiveCastRange, skill.areaRadius);
                case SkillTargetType.BackmostEnemy:
                    return BattleAiDirector.SelectBackmostEnemyTarget(context.Heroes, caster, effectiveCastRange);
                default:
                    return null;
            }
        }

        private static RuntimeHero SelectUltimatePrimaryTarget(BattleContext context, RuntimeHero caster, SkillData skill)
        {
            var decision = skill?.ultimateDecision;
            var effectiveCastRange = GetSkillSelectionCastRange(skill);
            if (decision == null)
            {
                return SelectPrimaryTarget(context, caster, skill);
            }

            switch (decision.targetingType)
            {
                case UltimateTargetingType.CurrentTarget:
                    return caster.CurrentTarget != null && !caster.CurrentTarget.IsDead
                        ? caster.CurrentTarget
                        : SelectPrimaryTarget(context, caster, skill);
                case UltimateTargetingType.LowestHealthEnemyInRange:
                    return FindLowestHealth(context.Heroes, caster, skill, includeAllies: false, effectiveCastRange);
                case UltimateTargetingType.LowestHealthAllyInRange:
                    return FindLowestHealth(context.Heroes, caster, skill, includeAllies: true, effectiveCastRange);
                case UltimateTargetingType.EnemyDensestPosition:
                    return FindDensestEnemyAnchor(context.Heroes, caster, effectiveCastRange, skill.areaRadius);
                case UltimateTargetingType.Self:
                    return caster;
                case UltimateTargetingType.UseSkillTargetType:
                default:
                    return SelectPrimaryTarget(context, caster, skill);
            }
        }

        private static RuntimeHero SelectSequencePrimaryTarget(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero preferredTarget,
            CombatActionSequenceTargetRefreshMode targetRefreshMode,
            float effectiveCastRange)
        {
            var preferredTargetIsValid = IsPrimaryTargetStillValidForCastRange(skill, caster, preferredTarget, effectiveCastRange);
            if (targetRefreshMode != CombatActionSequenceTargetRefreshMode.RefreshEveryIteration && preferredTargetIsValid)
            {
                return preferredTarget;
            }

            if (targetRefreshMode == CombatActionSequenceTargetRefreshMode.KeepCurrentTarget)
            {
                return null;
            }

            return skill.targetType switch
            {
                SkillTargetType.Self => caster,
                SkillTargetType.AllAllies => FindFirstGlobalTeamTarget(context.Heroes, caster, skill, includeAllies: true),
                SkillTargetType.AllEnemies => FindFirstGlobalTeamTarget(context.Heroes, caster, skill, includeAllies: false),
                SkillTargetType.NearestEnemy => SelectCurrentOrNearestEnemyTarget(context, caster, skill, effectiveCastRange),
                SkillTargetType.LowestHealthEnemy => FindLowestHealth(context.Heroes, caster, skill, includeAllies: false, effectiveCastRange),
                SkillTargetType.LowestHealthAlly => FindLowestHealth(context.Heroes, caster, skill, includeAllies: true, effectiveCastRange),
                SkillTargetType.HighestDamageEnemyInRange => BattleAiDirector.SelectHighestDamageEnemyTarget(context.Heroes, caster, effectiveCastRange),
                SkillTargetType.DensestEnemyArea => FindDensestEnemyAnchor(context.Heroes, caster, effectiveCastRange, skill.areaRadius),
                SkillTargetType.BackmostEnemy => BattleAiDirector.SelectBackmostEnemyTarget(context.Heroes, caster, effectiveCastRange),
                _ => null,
            };
        }

        private static RuntimeHero SelectCurrentOrNearestEnemyTarget(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            float effectiveCastRange)
        {
            if (context == null || caster == null || skill == null)
            {
                return null;
            }

            var currentTarget = caster.CurrentTarget;
            if (currentTarget != null
                && IsValidTargetForSkill(skill, caster, currentTarget)
                && IsPrimaryTargetStillValidForCastRange(skill, caster, currentTarget, effectiveCastRange))
            {
                return currentTarget;
            }

            return BattleAiDirector.SelectNearestEnemyTarget(context.Heroes, caster, effectiveCastRange);
        }

        private static List<RuntimeHero> CollectTargets(BattleContext context, RuntimeHero caster, SkillData skill, RuntimeHero primaryTarget)
        {
            var results = new List<RuntimeHero>();
            if (context == null || caster == null || skill == null)
            {
                return results;
            }

            if (IsGlobalTeamTargeting(skill.targetType))
            {
                for (var i = 0; i < context.Heroes.Count; i++)
                {
                    var candidate = context.Heroes[i];
                    if (candidate.IsDead)
                    {
                        continue;
                    }

                    if (!IsValidGlobalTeamTarget(skill, caster, candidate))
                    {
                        continue;
                    }

                    results.Add(candidate);
                }

                return results;
            }

            if (primaryTarget == null)
            {
                return results;
            }

            if (skill.areaRadius <= 0f)
            {
                if (!IsPrimaryTargetStillValid(skill, caster, primaryTarget))
                {
                    return results;
                }

                results.Add(primaryTarget);
                return results;
            }

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate.IsDead)
                {
                    continue;
                }

                if (!IsValidTargetForSkill(skill, caster, candidate))
                {
                    continue;
                }

                if (Vector3.Distance(candidate.CurrentPosition, primaryTarget.CurrentPosition) <= skill.areaRadius)
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static List<RuntimeHero> CollectTargetsForCastRange(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            float effectiveCastRange)
        {
            var results = new List<RuntimeHero>();
            if (context == null || caster == null || skill == null)
            {
                return results;
            }

            if (IsGlobalTeamTargeting(skill.targetType))
            {
                for (var i = 0; i < context.Heroes.Count; i++)
                {
                    var candidate = context.Heroes[i];
                    if (candidate.IsDead)
                    {
                        continue;
                    }

                    if (!IsValidGlobalTeamTarget(skill, caster, candidate))
                    {
                        continue;
                    }

                    results.Add(candidate);
                }

                return results;
            }

            if (primaryTarget == null)
            {
                return results;
            }

            if (skill.areaRadius <= 0f)
            {
                if (!IsPrimaryTargetStillValidForCastRange(skill, caster, primaryTarget, effectiveCastRange))
                {
                    return results;
                }

                results.Add(primaryTarget);
                return results;
            }

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate.IsDead)
                {
                    continue;
                }

                if (!IsValidTargetForSkill(skill, caster, candidate))
                {
                    continue;
                }

                if (Vector3.Distance(candidate.CurrentPosition, primaryTarget.CurrentPosition) <= skill.areaRadius)
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static bool IsPrimaryTargetStillValid(SkillData skill, RuntimeHero caster, RuntimeHero primaryTarget)
        {
            return IsPrimaryTargetStillValidForCastRange(
                skill,
                caster,
                primaryTarget,
                GetSkillSelectionCastRange(skill));
        }

        private static bool IsPrimaryTargetStillValidForCastRange(
            SkillData skill,
            RuntimeHero caster,
            RuntimeHero primaryTarget,
            float effectiveCastRange)
        {
            if (skill == null || caster == null)
            {
                return false;
            }

            if (primaryTarget == null)
            {
                return AllowsMissingPrimaryTarget(skill);
            }

            if (primaryTarget.IsDead)
            {
                return false;
            }

            if (skill.targetType == SkillTargetType.Self)
            {
                return true;
            }

            if (skill.targetType == SkillTargetType.AllAllies || skill.targetType == SkillTargetType.AllEnemies)
            {
                return true;
            }

            if (Vector3.Distance(caster.CurrentPosition, primaryTarget.CurrentPosition) > effectiveCastRange)
            {
                return false;
            }

            return IsDirectTargetAllowed(skill, caster, primaryTarget);
        }

        private static bool IsValidTargetForSkill(SkillData skill, RuntimeHero caster, RuntimeHero candidate)
        {
            switch (skill.targetType)
            {
                case SkillTargetType.Self:
                    return candidate == caster;
                case SkillTargetType.LowestHealthAlly:
                case SkillTargetType.AllAllies:
                    return candidate.Side == caster.Side;
                case SkillTargetType.AllEnemies:
                    return candidate.Side != caster.Side;
            }

            var targetAllies = skill.skillType == SkillType.SingleTargetHeal
                || skill.skillType == SkillType.AreaHeal;
            return targetAllies
                ? candidate.Side == caster.Side
                : candidate.Side != caster.Side;
        }

        private static bool HasHighValueOpportunity(SkillData skill, List<RuntimeHero> affectedTargets)
        {
            if (skill.skillType == SkillType.AreaDamage || skill.skillType == SkillType.Stun || skill.skillType == SkillType.KnockUp)
            {
                return affectedTargets.Count >= Mathf.Max(2, skill.minTargetsToCast);
            }

            if (skill.skillType == SkillType.SingleTargetHeal || skill.skillType == SkillType.AreaHeal || skill.skillType == SkillType.Buff)
            {
                return affectedTargets.Count > 0;
            }

            return affectedTargets.Count > 0;
        }

        private static bool EvaluateUltimateDecision(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            List<RuntimeHero> affectedTargets)
        {
            var decision = skill?.ultimateDecision;
            if (decision == null || decision.primaryCondition == null)
            {
                return false;
            }

            var primaryPass = EvaluateUltimateCondition(context, caster, skill, primaryTarget, decision.primaryCondition, decision.fallback, affectedTargets);
            if (decision.combineMode == UltimateConditionCombineMode.PrimaryOnly || decision.secondaryCondition == null || decision.secondaryCondition.conditionType == UltimateConditionType.None)
            {
                return primaryPass;
            }

            // Stage-01 fallback thresholds are defined as "primary condition fallback".
            // Secondary conditions stay stable so AnyPass/AllMustPass do not pick up hidden
            // threshold changes when the primary condition relaxes later in the match.
            var secondaryPass = EvaluateUltimateCondition(context, caster, skill, primaryTarget, decision.secondaryCondition, null, affectedTargets);
            return decision.combineMode switch
            {
                UltimateConditionCombineMode.AllMustPass => primaryPass && secondaryPass,
                UltimateConditionCombineMode.AnyPass => primaryPass || secondaryPass,
                _ => primaryPass,
            };
        }

        private static bool EvaluateUltimateCondition(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            UltimateConditionData condition,
            UltimateFallbackData fallback,
            List<RuntimeHero> affectedTargets)
        {
            if (condition == null || condition.conditionType == UltimateConditionType.None)
            {
                return true;
            }

            var searchRadius = GetEffectiveSearchRadius(skill, condition);
            var requiredUnitCount = GetEffectiveRequiredUnitCount(condition, fallback, context);
            var healthThreshold = GetEffectiveHealthPercentThreshold(condition, fallback, context);

            switch (condition.conditionType)
            {
                case UltimateConditionType.EnemyCountInRange:
                    return CountUnitsInRange(context, caster, primaryTarget, searchRadius, countAllies: false) >= requiredUnitCount;
                case UltimateConditionType.AllyCountInRange:
                    return CountUnitsInRange(context, caster, primaryTarget, searchRadius, countAllies: true) >= requiredUnitCount;
                case UltimateConditionType.EnemyLowHealthInRange:
                    return CountLowHealthUnitsInRange(context, caster, primaryTarget, searchRadius, includeAllies: false, healthThreshold) >= requiredUnitCount;
                case UltimateConditionType.AllyLowHealthInRange:
                    return CountLowHealthUnitsInRange(context, caster, primaryTarget, searchRadius, includeAllies: true, healthThreshold) >= requiredUnitCount;
                case UltimateConditionType.SelfLowHealth:
                    return GetHealthRatio(caster) <= healthThreshold;
                case UltimateConditionType.TargetIsHighValue:
                    return IsHighValueTarget(primaryTarget, caster, skill, condition);
                case UltimateConditionType.InCombatDuration:
                    return caster.CombatEngagedSeconds >= condition.durationSeconds;
                default:
                    return affectedTargets != null && affectedTargets.Count > 0;
            }
        }

        private static bool RollUltimateCastChance(BattleContext context, RuntimeHero caster, SkillData skill, RuntimeHero primaryTarget)
        {
            var chance = GetUltimateCastChance(context, caster, skill, primaryTarget);
            chance = ApplyAllyUltimateSuppression(context, caster, chance);
            ScheduleNextUltimateAttempt(context, caster);
            return context?.RandomService == null || context.RandomService.NextFloat() <= chance;
        }

        private static bool RollLegacyUltimateCastChance(BattleContext context, RuntimeHero caster, SkillData skill, List<RuntimeHero> affectedTargets)
        {
            var chance = Mathf.Clamp01(UltimateBaseReleaseChance + Mathf.Max(0, affectedTargets.Count - Mathf.Max(1, skill.minTargetsToCast)) * UltimateExtraUnitReleaseChance);
            chance = ApplyAllyUltimateSuppression(context, caster, chance);
            ScheduleNextUltimateAttempt(context, caster);
            return context?.RandomService == null || context.RandomService.NextFloat() <= chance;
        }

        private static float GetUltimateCastChance(BattleContext context, RuntimeHero caster, SkillData skill, RuntimeHero primaryTarget)
        {
            var decision = skill?.ultimateDecision;
            var primaryCondition = decision?.primaryCondition;
            if (primaryCondition == null)
            {
                return 1f;
            }

            var chance = UltimateBaseReleaseChance;
            var fallbackStage = GetActiveFallbackStage(context, decision?.fallback);
            if (fallbackStage >= 1)
            {
                chance += UltimateFirstFallbackBonus;
            }

            if (fallbackStage >= 2)
            {
                chance += UltimateSecondFallbackBonus;
            }

            switch (primaryCondition.conditionType)
            {
                case UltimateConditionType.EnemyCountInRange:
                case UltimateConditionType.AllyCountInRange:
                case UltimateConditionType.EnemyLowHealthInRange:
                case UltimateConditionType.AllyLowHealthInRange:
                    var measuredCount = GetConditionUnitCount(context, caster, skill, primaryTarget, primaryCondition);
                    var requiredCount = GetEffectiveRequiredUnitCount(primaryCondition, decision?.fallback, context);
                    chance += Mathf.Max(0, measuredCount - requiredCount) * UltimateExtraUnitReleaseChance;
                    break;
                case UltimateConditionType.SelfLowHealth:
                    chance += Mathf.Clamp01((primaryCondition.healthPercentThreshold - GetHealthRatio(caster)) * 1.5f);
                    break;
                case UltimateConditionType.InCombatDuration:
                    chance += Mathf.Clamp01((caster.CombatEngagedSeconds - primaryCondition.durationSeconds) * 0.1f);
                    break;
                case UltimateConditionType.TargetIsHighValue:
                    chance += 0.1f;
                    break;
            }

            var secondaryCondition = decision?.secondaryCondition;
            if (decision != null
                && decision.combineMode == UltimateConditionCombineMode.PrimaryOnly
                && secondaryCondition != null
                && secondaryCondition.conditionType != UltimateConditionType.None
                && EvaluateUltimateCondition(context, caster, skill, primaryTarget, secondaryCondition, null, null))
            {
                // Stage-01 uses PrimaryOnly + secondaryCondition to express
                // "the main cast gate is unchanged, but this opportunity is more attractive".
                chance += UltimateSecondaryPriorityBonus;
            }

            return Mathf.Clamp01(chance);
        }

        private static float ApplyAllyUltimateSuppression(BattleContext context, RuntimeHero caster, float chance)
        {
            if (context?.Clock == null || caster == null || caster.Side == TeamSide.None)
            {
                return Mathf.Clamp01(chance);
            }

            var lastAllyUltimateCastTimeSeconds = context.GetLastUltimateCastTimeSeconds(caster.Side);
            if (float.IsNegativeInfinity(lastAllyUltimateCastTimeSeconds))
            {
                return Mathf.Clamp01(chance);
            }

            var timeSinceLastAllyUltimateSeconds = Mathf.Max(0f, context.Clock.ElapsedTimeSeconds - lastAllyUltimateCastTimeSeconds);
            if (timeSinceLastAllyUltimateSeconds > UltimateAllySuppressionWindowSeconds)
            {
                return Mathf.Clamp01(chance);
            }

            return Mathf.Clamp01(chance * UltimateAllySuppressionChanceMultiplier);
        }

        private static int GetConditionUnitCount(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            UltimateConditionData condition)
        {
            var searchRadius = GetEffectiveSearchRadius(skill, condition);
            var healthThreshold = GetEffectiveHealthPercentThreshold(condition, null, context);

            return condition.conditionType switch
            {
                UltimateConditionType.EnemyCountInRange => CountUnitsInRange(context, caster, primaryTarget, searchRadius, countAllies: false),
                UltimateConditionType.AllyCountInRange => CountUnitsInRange(context, caster, primaryTarget, searchRadius, countAllies: true),
                UltimateConditionType.EnemyLowHealthInRange => CountLowHealthUnitsInRange(context, caster, primaryTarget, searchRadius, includeAllies: false, healthThreshold),
                UltimateConditionType.AllyLowHealthInRange => CountLowHealthUnitsInRange(context, caster, primaryTarget, searchRadius, includeAllies: true, healthThreshold),
                _ => 0,
            };
        }

        private static int GetActiveFallbackStage(BattleContext context, UltimateFallbackData fallback)
        {
            if (context?.Clock == null || fallback == null || fallback.fallbackType != UltimateFallbackType.LowerPrimaryThreshold)
            {
                return 0;
            }

            var elapsedTime = context.Clock.ElapsedTimeSeconds;
            if (fallback.secondaryTriggerAfterSeconds > 0f && elapsedTime >= fallback.secondaryTriggerAfterSeconds)
            {
                return 2;
            }

            if (fallback.triggerAfterSeconds > 0f && elapsedTime >= fallback.triggerAfterSeconds)
            {
                return 1;
            }

            return 0;
        }

        private static float GetEffectiveSearchRadius(SkillData skill, UltimateConditionData condition)
        {
            if (condition != null && condition.searchRadius > 0f)
            {
                return condition.searchRadius;
            }

            if (skill != null && skill.areaRadius > 0f)
            {
                return skill.areaRadius;
            }

            return skill != null ? skill.castRange : 0f;
        }

        private static int GetEffectiveRequiredUnitCount(UltimateConditionData condition, UltimateFallbackData fallback, BattleContext context)
        {
            var defaultValue = Mathf.Max(1, condition?.requiredUnitCount ?? 1);
            if (condition == null || fallback == null || fallback.fallbackType != UltimateFallbackType.LowerPrimaryThreshold)
            {
                return defaultValue;
            }

            if (context == null || context.Clock == null || context.Clock.ElapsedTimeSeconds < fallback.triggerAfterSeconds)
            {
                return defaultValue;
            }

            if (fallback.secondaryTriggerAfterSeconds > 0f
                && context.Clock.ElapsedTimeSeconds >= fallback.secondaryTriggerAfterSeconds
                && fallback.secondaryOverrideRequiredUnitCount > 0)
            {
                return Mathf.Max(1, fallback.secondaryOverrideRequiredUnitCount);
            }

            return fallback.overrideRequiredUnitCount > 0
                ? Mathf.Max(1, fallback.overrideRequiredUnitCount)
                : defaultValue;
        }

        private static float GetEffectiveHealthPercentThreshold(UltimateConditionData condition, UltimateFallbackData fallback, BattleContext context)
        {
            var defaultValue = Mathf.Clamp01(condition?.healthPercentThreshold ?? 1f);
            if (condition == null || fallback == null || fallback.fallbackType != UltimateFallbackType.LowerPrimaryThreshold)
            {
                return defaultValue;
            }

            if (context == null || context.Clock == null || context.Clock.ElapsedTimeSeconds < fallback.triggerAfterSeconds)
            {
                return defaultValue;
            }

            if (fallback.secondaryTriggerAfterSeconds > 0f
                && context.Clock.ElapsedTimeSeconds >= fallback.secondaryTriggerAfterSeconds
                && fallback.secondaryOverrideHealthPercentThreshold >= 0f)
            {
                return Mathf.Clamp01(fallback.secondaryOverrideHealthPercentThreshold);
            }

            return fallback.overrideHealthPercentThreshold >= 0f
                ? Mathf.Clamp01(fallback.overrideHealthPercentThreshold)
                : defaultValue;
        }

        private static int CountUnitsInRange(BattleContext context, RuntimeHero caster, RuntimeHero primaryTarget, float searchRadius, bool countAllies)
        {
            var center = GetSearchCenter(caster, primaryTarget);
            var count = 0;
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate.IsDead)
                {
                    continue;
                }

                var isAlly = candidate.Side == caster.Side;
                if (countAllies != isAlly)
                {
                    continue;
                }

                if (Vector3.Distance(candidate.CurrentPosition, center) <= searchRadius)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountLowHealthUnitsInRange(
            BattleContext context,
            RuntimeHero caster,
            RuntimeHero primaryTarget,
            float searchRadius,
            bool includeAllies,
            float healthPercentThreshold)
        {
            var center = GetSearchCenter(caster, primaryTarget);
            var count = 0;
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate.IsDead)
                {
                    continue;
                }

                var isAlly = candidate.Side == caster.Side;
                if (includeAllies != isAlly)
                {
                    continue;
                }

                if (Vector3.Distance(candidate.CurrentPosition, center) > searchRadius)
                {
                    continue;
                }

                if (GetHealthRatio(candidate) <= healthPercentThreshold)
                {
                    count++;
                }
            }

            return count;
        }

        private static Vector3 GetSearchCenter(RuntimeHero caster, RuntimeHero primaryTarget)
        {
            return primaryTarget != null ? primaryTarget.CurrentPosition : caster.CurrentPosition;
        }

        private static float GetHealthRatio(RuntimeHero hero)
        {
            return hero != null && hero.MaxHealth > 0f
                ? hero.CurrentHealth / hero.MaxHealth
                : 1f;
        }

        private static bool IsHighValueTarget(RuntimeHero target, RuntimeHero caster, SkillData skill, UltimateConditionData condition)
        {
            if (target == null || target.IsDead)
            {
                return false;
            }

            if (condition.requireTargetInCastRange && Vector3.Distance(caster.CurrentPosition, target.CurrentPosition) > skill.castRange)
            {
                return false;
            }

            return condition.highValueTargetType switch
            {
                HighValueTargetType.Backline => target.Definition.heroClass is HeroClass.Mage or HeroClass.Support or HeroClass.Marksman,
                HighValueTargetType.Ranged => target.Definition.heroClass is HeroClass.Mage or HeroClass.Support or HeroClass.Marksman,
                HighValueTargetType.LowDefense => target.Defense <= 15f,
                HighValueTargetType.LowHealth => GetHealthRatio(target) <= 0.5f,
                _ => true,
            };
        }

        public static void ResolvePendingSkillCast(BattleContext context, RuntimeHero caster, PendingCombatAction pendingAction, BattleManager battleManager)
        {
            if (context == null || caster == null || caster.IsDead || pendingAction == null || pendingAction.Skill == null || battleManager == null)
            {
                return;
            }

            ResolveSkillEffects(
                context,
                caster,
                pendingAction.Skill,
                pendingAction.PrimaryTarget,
                CopyAliveTargets(pendingAction.AffectedTargets),
                battleManager,
                !pendingAction.SuppressActionSequenceTrigger);
        }

        internal static bool TryPrepareSequenceSkillCast(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero preferredTarget,
            CombatActionSequenceTargetRefreshMode targetRefreshMode,
            float castRangeOverride,
            out RuntimeHero primaryTarget,
            out List<RuntimeHero> affectedTargets)
        {
            primaryTarget = null;
            affectedTargets = new List<RuntimeHero>();
            if (context == null || caster == null || skill == null)
            {
                return false;
            }

            var effectiveCastRange = GetSequenceCastRange(skill, castRangeOverride);
            primaryTarget = SelectSequencePrimaryTarget(
                context,
                caster,
                skill,
                preferredTarget,
                targetRefreshMode,
                effectiveCastRange);
            if (!IsPrimaryTargetStillValidForCastRange(skill, caster, primaryTarget, effectiveCastRange))
            {
                return false;
            }

            if (primaryTarget == null && !skill.allowsSelfCast && !AllowsMissingPrimaryTarget(skill))
            {
                return false;
            }

            affectedTargets = CollectTargetsForCastRange(context, caster, skill, primaryTarget, effectiveCastRange);
            return affectedTargets.Count >= Mathf.Max(1, skill.minTargetsToCast);
        }

        public static void TickDelayedSkillEffects(BattleContext context, float deltaTime, BattleManager battleManager)
        {
            if (context?.DelayedSkillEffects == null || battleManager == null)
            {
                return;
            }

            for (var i = context.DelayedSkillEffects.Count - 1; i >= 0; i--)
            {
                var delayedEffect = context.DelayedSkillEffects[i];
                if (delayedEffect == null)
                {
                    context.DelayedSkillEffects.RemoveAt(i);
                    continue;
                }

                delayedEffect.Tick(deltaTime);
                if (!delayedEffect.IsReady)
                {
                    continue;
                }

                context.DelayedSkillEffects.RemoveAt(i);
                ResolveDelayedSkillEffect(context, delayedEffect, battleManager);
            }
        }

        private static void BeginSkillCast(BattleContext context, RuntimeHero caster, SkillData skill, RuntimeHero primaryTarget, List<RuntimeHero> affectedTargets, BattleManager battleManager)
        {
            QueueSkillCast(
                context,
                caster,
                skill,
                primaryTarget,
                affectedTargets,
                CombatActionTiming.DefaultWindupSeconds,
                CombatActionTiming.DefaultRecoverySeconds,
                consumeCooldown: true,
                suppressActionSequenceTrigger: false,
                isActionSequenceStep: false,
                battleManager);
        }

        internal static void BeginSequenceSkillCast(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            List<RuntimeHero> affectedTargets,
            float windupSeconds,
            float recoverySeconds,
            BattleManager battleManager)
        {
            QueueSkillCast(
                context,
                caster,
                skill,
                primaryTarget,
                affectedTargets,
                windupSeconds,
                recoverySeconds,
                consumeCooldown: false,
                suppressActionSequenceTrigger: true,
                isActionSequenceStep: true,
                battleManager);
        }

        private static void QueueSkillCast(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            List<RuntimeHero> affectedTargets,
            float windupSeconds,
            float recoverySeconds,
            bool consumeCooldown,
            bool suppressActionSequenceTrigger,
            bool isActionSequenceStep,
            BattleManager battleManager)
        {
            if (context == null || caster == null || skill == null || battleManager == null)
            {
                return;
            }

            caster.BeginSkillCast(
                skill,
                primaryTarget,
                affectedTargets,
                windupSeconds,
                recoverySeconds,
                consumeCooldown,
                suppressActionSequenceTrigger,
                isActionSequenceStep);

            // Mark the team's latest committed ultimate so allied ult rolls can stagger.
            if (consumeCooldown && skill.slotType == SkillSlotType.Ultimate)
            {
                context.RecordUltimateCast(caster.Side);
            }

            context.EventBus.Publish(new SkillCastEvent(
                caster,
                skill,
                primaryTarget,
                GetSkillCastAffectedTargetCount(context, caster, skill, primaryTarget, affectedTargets)));
        }

        private static void ResolveSkillEffects(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            List<RuntimeHero> affectedTargets,
            BattleManager battleManager,
            bool allowActionSequenceTrigger = true)
        {
            if (context == null || caster == null || caster.IsDead || skill == null || battleManager == null)
            {
                return;
            }

            if (skill.effects != null && skill.effects.Count > 0)
            {
                var resolutionState = CreateSkillEffectResolutionState(skill, caster, primaryTarget);
                for (var i = 0; i < skill.effects.Count; i++)
                {
                    ExecuteSkillEffect(context, caster, skill, primaryTarget, affectedTargets, skill.effects[i], resolutionState, battleManager);
                }
            }
            else
            {
                switch (skill.skillType)
                {
                    case SkillType.SingleTargetDamage:
                    case SkillType.AreaDamage:
                    case SkillType.Dash:
                    case SkillType.Stun:
                    case SkillType.KnockUp:
                        ApplyDamageToTargets(context, caster, skill, CreateLegacyDamageEffect(skill), affectedTargets, battleManager);
                        break;
                    case SkillType.SingleTargetHeal:
                    case SkillType.AreaHeal:
                        ApplyHealToTargets(context, caster, skill, CreateLegacyHealEffect(skill), affectedTargets);
                        break;
                    case SkillType.Buff:
                        ApplyStatusEffectsToTargets(context, caster, skill, CreateLegacyStatusEffect(skill), affectedTargets);
                        break;
                }
            }

            if (allowActionSequenceTrigger)
            {
                BattleCombatActionSequenceSystem.TryStartSequence(caster, skill, primaryTarget);
            }
        }

        private static List<RuntimeHero> CopyAliveTargets(IReadOnlyList<RuntimeHero> targets)
        {
            var results = new List<RuntimeHero>();
            if (targets == null)
            {
                return results;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || target.IsDead)
                {
                    continue;
                }

                results.Add(target);
            }

            return results;
        }

        private static int GetSkillCastAffectedTargetCount(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            List<RuntimeHero> affectedTargets)
        {
            if (context == null || caster == null || skill == null || skill.effects == null || skill.effects.Count <= 0)
            {
                return affectedTargets != null ? affectedTargets.Count : 0;
            }

            var resolutionState = CreateSkillEffectResolutionState(skill, caster, primaryTarget);
            var uniqueTargetIds = new HashSet<string>();

            for (var i = 0; i < skill.effects.Count; i++)
            {
                var effectTargets = ResolveEffectTargets(context, caster, skill, primaryTarget, affectedTargets, skill.effects[i], resolutionState);
                for (var j = 0; j < effectTargets.Count; j++)
                {
                    var target = effectTargets[j];
                    if (target == null)
                    {
                        continue;
                    }

                    uniqueTargetIds.Add(target.RuntimeId);
                }
            }

            return uniqueTargetIds.Count > 0
                ? uniqueTargetIds.Count
                : (affectedTargets != null ? affectedTargets.Count : 0);
        }

        private static SkillEffectResolutionState CreateSkillEffectResolutionState(SkillData skill, RuntimeHero caster, RuntimeHero primaryTarget)
        {
            var dashStartPosition = caster != null ? caster.CurrentPosition : Vector3.zero;
            if (caster == null || primaryTarget == null || !TryGetDashRepositionEffect(skill, out var dashEffect))
            {
                return new SkillEffectResolutionState(dashStartPosition, dashStartPosition, 0f, false);
            }

            return new SkillEffectResolutionState(
                dashStartPosition,
                GetDashDestination(dashStartPosition, primaryTarget.CurrentPosition),
                dashEffect.durationSeconds,
                true);
        }

        private static bool TryGetDashRepositionEffect(SkillData skill, out SkillEffectData dashEffect)
        {
            dashEffect = null;
            if (skill?.effects == null)
            {
                return false;
            }

            for (var i = 0; i < skill.effects.Count; i++)
            {
                if (skill.effects[i] != null && skill.effects[i].effectType == SkillEffectType.RepositionNearPrimaryTarget)
                {
                    dashEffect = skill.effects[i];
                    return true;
                }
            }

            return false;
        }

        private static List<RuntimeHero> ResolveEffectTargets(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            List<RuntimeHero> affectedTargets,
            SkillEffectData effect,
            SkillEffectResolutionState resolutionState)
        {
            if (effect == null)
            {
                return new List<RuntimeHero>();
            }

            switch (effect.targetMode)
            {
                case SkillEffectTargetMode.Caster:
                    return CollectCasterTarget(caster);
                case SkillEffectTargetMode.PrimaryTarget:
                    return CollectPrimaryEffectTarget(primaryTarget, caster);
                case SkillEffectTargetMode.EnemiesInRadiusAroundCaster:
                    return CollectUnitsInRadius(context, caster, caster.CurrentPosition, GetEffectRadius(skill, effect), includeAllies: false);
                case SkillEffectTargetMode.AlliesInRadiusAroundCaster:
                    return CollectUnitsInRadius(context, caster, caster.CurrentPosition, GetEffectRadius(skill, effect), includeAllies: true);
                case SkillEffectTargetMode.DashPathEnemies:
                    return CollectDashPathTargets(context, caster, skill, effect, resolutionState);
                case SkillEffectTargetMode.SkillTargets:
                default:
                    return CopyAliveTargets(affectedTargets);
            }
        }

        private static void ExecuteSkillEffect(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            List<RuntimeHero> affectedTargets,
            SkillEffectData effect,
            SkillEffectResolutionState resolutionState,
            BattleManager battleManager)
        {
            if (ShouldDelaySkillEffect(effect, resolutionState))
            {
                ScheduleDelayedSkillEffect(context, caster, skill, primaryTarget, affectedTargets, effect, resolutionState);
                return;
            }

            ExecuteSkillEffectNow(context, caster, skill, primaryTarget, affectedTargets, effect, resolutionState, battleManager);
        }

        private static void ExecuteSkillEffectNow(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            List<RuntimeHero> affectedTargets,
            SkillEffectData effect,
            SkillEffectResolutionState resolutionState,
            BattleManager battleManager)
        {
            var effectTargets = ResolveEffectTargets(context, caster, skill, primaryTarget, affectedTargets, effect, resolutionState);
            switch (effect.effectType)
            {
                case SkillEffectType.DirectDamage:
                    ApplyDamageToTargets(context, caster, skill, effect, effectTargets, battleManager);
                    break;
                case SkillEffectType.DirectHeal:
                    ApplyHealToTargets(context, caster, skill, effect, effectTargets);
                    break;
                case SkillEffectType.ApplyStatusEffects:
                    ApplyStatusEffectsToTargets(context, caster, skill, effect, effectTargets);
                    break;
                case SkillEffectType.ApplyForcedMovement:
                    ApplyForcedMovementToTargets(context, caster, skill, effect, effectTargets);
                    break;
                case SkillEffectType.RepositionNearPrimaryTarget:
                    if (primaryTarget != null)
                    {
                        ApplyDashReposition(context, caster, primaryTarget, skill, effect, resolutionState);
                    }
                    break;
                case SkillEffectType.CreatePersistentArea:
                    CreatePersistentSkillArea(context, caster, skill, effect, primaryTarget);
                    break;
            }
        }

        private static bool ShouldDelaySkillEffect(SkillEffectData effect, SkillEffectResolutionState resolutionState)
        {
            return effect != null
                && effect.targetMode == SkillEffectTargetMode.DashPathEnemies
                && resolutionState.HasDashPath
                && resolutionState.DashDurationSeconds > Mathf.Epsilon;
        }

        private static void ScheduleDelayedSkillEffect(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            List<RuntimeHero> affectedTargets,
            SkillEffectData effect,
            SkillEffectResolutionState resolutionState)
        {
            if (context?.DelayedSkillEffects == null || effect == null)
            {
                return;
            }

            context.DelayedSkillEffects.Add(new RuntimeDelayedSkillEffect(
                caster,
                skill,
                primaryTarget,
                affectedTargets,
                effect,
                resolutionState,
                resolutionState.DashDurationSeconds));
        }

        private static void ResolveDelayedSkillEffect(BattleContext context, RuntimeDelayedSkillEffect delayedEffect, BattleManager battleManager)
        {
            if (context == null
                || delayedEffect?.Caster == null
                || delayedEffect.Caster.IsDead
                || delayedEffect.Skill == null
                || delayedEffect.Effect == null
                || battleManager == null)
            {
                return;
            }

            ExecuteSkillEffectNow(
                context,
                delayedEffect.Caster,
                delayedEffect.Skill,
                delayedEffect.PrimaryTarget,
                CopyAliveTargets(delayedEffect.AffectedTargets),
                delayedEffect.Effect,
                delayedEffect.ResolutionState,
                battleManager);
        }

        public static void ResolveSkillAreaPulse(BattleContext context, RuntimeSkillArea area, BattleManager battleManager)
        {
            if (context == null || area == null || area.Skill == null || area.Caster == null)
            {
                return;
            }

            var targets = CollectAreaTargets(context, area.Caster, area.CurrentCenter, area.Skill, area.Effect);
            context.EventBus.Publish(new SkillAreaPulseEvent(area.Caster, area.Skill, area, targets.Count));
            switch (area.Effect.persistentAreaPulseEffectType)
            {
                case PersistentAreaPulseEffectType.DirectHeal:
                    ApplyHealToTargets(context, area.Caster, area.Skill, area.Effect, targets);
                    if (HasStatusPayload(area.Effect))
                    {
                        ApplyStatusEffectsToTargets(context, area.Caster, area.Skill, area.Effect, targets);
                    }

                    break;
                case PersistentAreaPulseEffectType.None:
                    ApplyStatusEffectsToTargets(context, area.Caster, area.Skill, area.Effect, targets);
                    break;
                case PersistentAreaPulseEffectType.DirectDamage:
                default:
                    ApplyDamageToTargets(context, area.Caster, area.Skill, area.Effect, targets, battleManager, DamageSourceKind.SkillAreaPulse);
                    break;
            }
        }

        private static void ApplyHealToTargets(BattleContext context, RuntimeHero caster, SkillData skill, SkillEffectData effect, List<RuntimeHero> targets)
        {
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var amount = HealResolver.ResolveHealAmount(caster, effect.powerMultiplier);
                var actualHeal = target.ApplyHealing(amount);
                if (actualHeal <= 0f)
                {
                    continue;
                }

                caster.RecordHealing(actualHeal);
                context.EventBus.Publish(new HealAppliedEvent(caster, target, actualHeal, skill, target.CurrentHealth));
            }
        }

        private static void ApplyStatusEffectsToTargets(BattleContext context, RuntimeHero caster, SkillData sourceSkill, SkillEffectData effect, List<RuntimeHero> targets)
        {
            for (var i = 0; i < targets.Count; i++)
            {
                ApplyStatuses(context, caster, sourceSkill, effect, targets[i]);
            }
        }

        private static void ApplyForcedMovementToTargets(BattleContext context, RuntimeHero caster, SkillData sourceSkill, SkillEffectData effect, List<RuntimeHero> targets)
        {
            if (effect == null)
            {
                return;
            }

            var distance = Mathf.Max(0f, effect.forcedMovementDistance);
            var durationSeconds = Mathf.Max(0f, effect.forcedMovementDurationSeconds);
            var peakHeight = Mathf.Max(0f, effect.forcedMovementPeakHeight);
            if (distance <= Mathf.Epsilon && durationSeconds <= Mathf.Epsilon)
            {
                return;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || target.IsDead)
                {
                    continue;
                }

                var startPosition = target.CurrentPosition;
                var destination = GetForcedMovementDestination(caster, target, effect, distance);
                target.StartForcedMovement(destination, durationSeconds, peakHeight);
                context.EventBus.Publish(new ForcedMovementAppliedEvent(
                    caster,
                    target,
                    startPosition,
                    destination,
                    durationSeconds,
                    peakHeight,
                    sourceSkill));
            }
        }

        private static void ApplyStatuses(BattleContext context, RuntimeHero caster, SkillData sourceSkill, SkillEffectData effect, RuntimeHero target)
        {
            if (effect == null || effect.statusEffects == null)
            {
                return;
            }

            for (var i = 0; i < effect.statusEffects.Count; i++)
            {
                var status = effect.statusEffects[i];
                if (!target.ApplyStatusEffect(status, caster, sourceSkill, caster, out var appliedStatus))
                {
                    continue;
                }

                context.EventBus.Publish(new StatusAppliedEvent(
                    appliedStatus?.Source ?? caster,
                    target,
                    status.effectType,
                    status.durationSeconds,
                    status.magnitude,
                    sourceSkill,
                    appliedStatus?.AppliedBy ?? caster));
            }
        }

        private static void CreatePersistentSkillArea(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            SkillEffectData effect,
            RuntimeHero primaryTarget)
        {
            var initialCenter = primaryTarget != null
                ? primaryTarget.CurrentPosition
                : caster.CurrentPosition;
            var area = new RuntimeSkillArea(caster, skill, effect, initialCenter);
            context.SkillAreas.Add(area);
            context.EventBus.Publish(new SkillAreaCreatedEvent(caster, skill, area));
        }

        private static List<RuntimeHero> CollectAreaTargets(BattleContext context, RuntimeHero caster, Vector3 center, SkillData skill, SkillEffectData effect)
        {
            var results = new List<RuntimeHero>();
            var radius = GetEffectRadius(skill, effect);
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate.IsDead)
                {
                    continue;
                }

                if (!IsValidPersistentAreaTarget(caster, candidate, effect))
                {
                    continue;
                }

                if (Vector3.Distance(candidate.CurrentPosition, center) <= radius)
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static void ApplyDamageToTargets(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            SkillEffectData effect,
            List<RuntimeHero> targets,
            BattleManager battleManager,
            DamageSourceKind damageSourceKind = DamageSourceKind.Skill)
        {
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var damage = DamageResolver.ResolveDamage(
                    caster.AttackPower,
                    caster.CriticalChance,
                    caster.CriticalDamageMultiplier,
                    target.Defense,
                    context.RandomService,
                    effect.powerMultiplier);
                var actualDamage = BattleDamageSystem.ApplyResolvedDamage(
                    context,
                    battleManager,
                    caster,
                    target,
                    damage,
                    damageSourceKind,
                    skill);
                if (actualDamage <= 0f)
                {
                    ApplyStatuses(context, caster, skill, effect, target);
                    continue;
                }

                if (!target.IsDead && target.CurrentHealth > 0f)
                {
                    ApplyStatuses(context, caster, skill, effect, target);
                    continue;
                }
            }
        }

        private static float GetEffectRadius(SkillData skill, SkillEffectData effect)
        {
            if (effect != null && effect.radiusOverride > 0f)
            {
                return effect.radiusOverride;
            }

            return skill != null ? skill.areaRadius : 0f;
        }

        private static List<RuntimeHero> CollectCasterTarget(RuntimeHero caster)
        {
            var results = new List<RuntimeHero>();
            if (caster != null && !caster.IsDead)
            {
                results.Add(caster);
            }

            return results;
        }

        private static List<RuntimeHero> CollectPrimaryEffectTarget(RuntimeHero primaryTarget, RuntimeHero caster)
        {
            var results = new List<RuntimeHero>();
            if (primaryTarget == null || primaryTarget.IsDead)
            {
                return results;
            }

            if (primaryTarget != caster && !primaryTarget.CanBeDirectTargeted)
            {
                return results;
            }

            results.Add(primaryTarget);
            return results;
        }

        private static List<RuntimeHero> CollectUnitsInRadius(
            BattleContext context,
            RuntimeHero caster,
            Vector3 center,
            float radius,
            bool includeAllies)
        {
            var results = new List<RuntimeHero>();
            if (context == null || caster == null)
            {
                return results;
            }

            var effectiveRadius = Mathf.Max(0f, radius);
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate == null || candidate.IsDead)
                {
                    continue;
                }

                var isAlly = candidate.Side == caster.Side;
                if (includeAllies != isAlly)
                {
                    continue;
                }

                if (Vector3.Distance(candidate.CurrentPosition, center) <= effectiveRadius)
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static List<RuntimeHero> CollectDashPathTargets(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            SkillEffectData effect,
            SkillEffectResolutionState resolutionState)
        {
            var results = new List<RuntimeHero>();
            if (context == null || caster == null || !resolutionState.HasDashPath)
            {
                return results;
            }

            var effectiveRadius = Mathf.Max(0.1f, GetEffectRadius(skill, effect));
            var start = resolutionState.DashStartPosition;
            var end = resolutionState.DashDestination;
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate == null || candidate.IsDead || candidate.Side == caster.Side)
                {
                    continue;
                }

                if (GetDistanceToSegment(candidate.CurrentPosition, start, end) <= effectiveRadius)
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static bool HasStatusPayload(SkillEffectData effect)
        {
            return effect?.statusEffects != null && effect.statusEffects.Count > 0;
        }

        private static bool IsValidPersistentAreaTarget(RuntimeHero caster, RuntimeHero candidate, SkillEffectData effect)
        {
            if (caster == null || candidate == null || effect == null)
            {
                return false;
            }

            return effect.persistentAreaTargetType switch
            {
                PersistentAreaTargetType.Allies => candidate.Side == caster.Side,
                PersistentAreaTargetType.Both => true,
                _ => candidate.Side != caster.Side,
            };
        }

        private static SkillEffectData CreateLegacyDamageEffect(SkillData skill)
        {
            return new SkillEffectData
            {
                effectType = SkillEffectType.DirectDamage,
                powerMultiplier = 1f,
            };
        }

        private static SkillEffectData CreateLegacyHealEffect(SkillData skill)
        {
            return new SkillEffectData
            {
                effectType = SkillEffectType.DirectHeal,
                powerMultiplier = 1f,
            };
        }

        private static SkillEffectData CreateLegacyStatusEffect(SkillData skill)
        {
            return new SkillEffectData
            {
                effectType = SkillEffectType.ApplyStatusEffects,
            };
        }

        private static RuntimeHero FindNearest(IReadOnlyList<RuntimeHero> heroes, RuntimeHero caster, bool includeAllies, float maxRange)
        {
            return includeAllies
                ? BattleAiDirector.SelectPreferredAllyTarget(heroes, caster, maxRange)
                : BattleAiDirector.SelectPreferredEnemyTarget(heroes, caster, maxRange);
        }

        private static bool IsGlobalTeamTargeting(SkillTargetType targetType)
        {
            return targetType == SkillTargetType.AllAllies || targetType == SkillTargetType.AllEnemies;
        }

        private static bool MatchesGlobalTeamTarget(SkillTargetType targetType, RuntimeHero caster, RuntimeHero candidate)
        {
            return targetType switch
            {
                SkillTargetType.AllAllies => candidate.Side == caster.Side,
                SkillTargetType.AllEnemies => candidate.Side != caster.Side,
                _ => false,
            };
        }

        private static bool IsValidGlobalTeamTarget(SkillData skill, RuntimeHero caster, RuntimeHero candidate)
        {
            if (skill == null || caster == null || candidate == null)
            {
                return false;
            }

            if (!MatchesGlobalTeamTarget(skill.targetType, caster, candidate))
            {
                return false;
            }

            return !RequiresDirectTargetValidation(skill) || IsDirectTargetAllowed(skill, caster, candidate);
        }

        private static RuntimeHero FindFirstGlobalTeamTarget(IReadOnlyList<RuntimeHero> heroes, RuntimeHero caster, SkillData skill, bool includeAllies)
        {
            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (candidate.IsDead)
                {
                    continue;
                }

                if (includeAllies && candidate.Side != caster.Side)
                {
                    continue;
                }

                if (!includeAllies && candidate.Side == caster.Side)
                {
                    continue;
                }

                if (RequiresDirectTargetValidation(skill) && !IsDirectTargetAllowed(skill, caster, candidate))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static bool RequiresDirectTargetValidation(SkillData skill)
        {
            if (skill == null)
            {
                return false;
            }

            if (skill.effects != null && skill.effects.Count > 0)
            {
                for (var i = 0; i < skill.effects.Count; i++)
                {
                    if (IsDirectUnitEffect(skill.effects[i]))
                    {
                        return true;
                    }
                }

                return false;
            }

            return skill.skillType != SkillType.AreaDamage && skill.skillType != SkillType.AreaHeal;
        }

        private static bool IsDirectUnitEffect(SkillEffectData effect)
        {
            if (effect == null)
            {
                return false;
            }

            return effect.effectType == SkillEffectType.DirectDamage
                || effect.effectType == SkillEffectType.DirectHeal
                || effect.effectType == SkillEffectType.ApplyStatusEffects
                || effect.effectType == SkillEffectType.ApplyForcedMovement;
        }

        private static bool IsDirectTargetAllowed(SkillData skill, RuntimeHero caster, RuntimeHero candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (candidate == caster && CanSkillTargetSelf(skill, caster))
            {
                return true;
            }

            return candidate.CanBeDirectTargeted;
        }

        private static bool CanSkillTargetSelf(SkillData skill, RuntimeHero caster)
        {
            if (skill == null || caster == null)
            {
                return false;
            }

            if (skill.targetType == SkillTargetType.Self
                || skill.targetType == SkillTargetType.LowestHealthAlly
                || skill.targetType == SkillTargetType.AllAllies)
            {
                return true;
            }

            return skill.allowsSelfCast && IsValidTargetForSkill(skill, caster, caster);
        }

        private static bool AllowsMissingPrimaryTarget(SkillData skill)
        {
            return skill != null && skill.targetType == SkillTargetType.DensestEnemyArea;
        }

        private static float GetSkillSelectionCastRange(SkillData skill)
        {
            if (skill == null)
            {
                return 0f;
            }

            var effectiveRange = Mathf.Max(0f, skill.castRange);
            var sequence = skill.actionSequence;
            if (sequence == null || !sequence.enabled)
            {
                return effectiveRange;
            }

            effectiveRange = Mathf.Max(effectiveRange, Mathf.Max(0f, sequence.temporarySkillCastRangeOverride));
            if (sequence.payloadType == CombatActionSequencePayloadType.BasicAttack)
            {
                effectiveRange = Mathf.Max(effectiveRange, Mathf.Max(0f, sequence.temporaryBasicAttackRangeOverride));
            }

            return effectiveRange;
        }

        private static float GetSequenceCastRange(SkillData skill, float castRangeOverride)
        {
            return Mathf.Max(
                skill != null ? Mathf.Max(0f, skill.castRange) : 0f,
                Mathf.Max(0f, castRangeOverride));
        }

        private static RuntimeHero FindLowestHealth(IReadOnlyList<RuntimeHero> heroes, RuntimeHero caster, SkillData skill, bool includeAllies, float maxRange)
        {
            RuntimeHero best = null;
            var lowestCurrentHealth = float.MaxValue;
            var lowestRatio = float.MaxValue;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (candidate.IsDead)
                {
                    continue;
                }

                if (!IsDirectTargetAllowed(skill, caster, candidate))
                {
                    continue;
                }

                if (includeAllies && candidate.Side != caster.Side)
                {
                    continue;
                }

                if (!includeAllies && candidate.Side == caster.Side)
                {
                    continue;
                }

                var distance = Vector3.Distance(caster.CurrentPosition, candidate.CurrentPosition);
                if (distance > maxRange)
                {
                    continue;
                }

                var ratio = candidate.MaxHealth > 0f ? candidate.CurrentHealth / candidate.MaxHealth : 1f;
                if (includeAllies)
                {
                    if (!IsBetterLowestAllyCandidate(candidate.CurrentHealth, ratio, distance, lowestCurrentHealth, lowestRatio, nearestDistance))
                    {
                        continue;
                    }

                    lowestCurrentHealth = candidate.CurrentHealth;
                    lowestRatio = ratio;
                    nearestDistance = distance;
                    best = candidate;
                    continue;
                }

                if (ratio >= lowestRatio)
                {
                    continue;
                }

                lowestRatio = ratio;
                best = candidate;
            }

            return best;
        }

        private static bool IsBetterLowestAllyCandidate(
            float currentHealth,
            float healthRatio,
            float distance,
            float bestCurrentHealth,
            float bestHealthRatio,
            float bestDistance)
        {
            if (currentHealth < bestCurrentHealth - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(currentHealth - bestCurrentHealth) > Mathf.Epsilon)
            {
                return false;
            }

            if (healthRatio < bestHealthRatio - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(healthRatio - bestHealthRatio) > Mathf.Epsilon)
            {
                return false;
            }

            return distance < bestDistance;
        }

        private static RuntimeHero FindDensestEnemyAnchor(IReadOnlyList<RuntimeHero> heroes, RuntimeHero caster, float maxRange, float radius)
        {
            RuntimeHero best = null;
            var bestCount = 0;
            var effectiveRadius = Mathf.Max(0.5f, radius);

            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (candidate.IsDead || candidate.Side == caster.Side || !candidate.CanBeDirectTargeted)
                {
                    continue;
                }

                var distance = Vector3.Distance(caster.CurrentPosition, candidate.CurrentPosition);
                if (distance > maxRange)
                {
                    continue;
                }

                var count = 0;
                for (var j = 0; j < heroes.Count; j++)
                {
                    var other = heroes[j];
                    if (other.IsDead || other.Side == caster.Side)
                    {
                        continue;
                    }

                    if (Vector3.Distance(candidate.CurrentPosition, other.CurrentPosition) <= effectiveRadius)
                    {
                        count++;
                    }
                }

                if (count <= bestCount)
                {
                    continue;
                }

                bestCount = count;
                best = candidate;
            }

            return best ?? BattleAiDirector.SelectPreferredEnemyTarget(heroes, caster, maxRange);
        }

        private static void ApplyDashReposition(
            BattleContext context,
            RuntimeHero caster,
            RuntimeHero target,
            SkillData sourceSkill,
            SkillEffectData effect,
            SkillEffectResolutionState resolutionState)
        {
            if (caster == null || target == null)
            {
                return;
            }

            var startPosition = caster.CurrentPosition;
            var destination = resolutionState.HasDashPath
                ? resolutionState.DashDestination
                : GetDashDestination(startPosition, target.CurrentPosition);
            var durationSeconds = effect != null
                ? Mathf.Max(0f, effect.durationSeconds > Mathf.Epsilon ? effect.durationSeconds : effect.forcedMovementDurationSeconds)
                : 0f;
            var peakHeight = effect != null ? Mathf.Max(0f, effect.forcedMovementPeakHeight) : 0f;
            caster.StartForcedMovement(destination, durationSeconds, peakHeight);
            context.EventBus.Publish(new ForcedMovementAppliedEvent(
                caster,
                caster,
                startPosition,
                destination,
                durationSeconds,
                peakHeight,
                sourceSkill));
        }

        private static Vector3 GetDashDestination(Vector3 casterPosition, Vector3 targetPosition)
        {
            var offset = targetPosition - casterPosition;
            if (offset.sqrMagnitude <= Mathf.Epsilon)
            {
                return Stage01ArenaSpec.ClampPosition(targetPosition);
            }

            var destination = targetPosition - offset.normalized * 0.8f;
            destination.y = 0f;
            return Stage01ArenaSpec.ClampPosition(destination);
        }

        private static Vector3 GetForcedMovementDestination(RuntimeHero caster, RuntimeHero target, SkillEffectData effect, float distance)
        {
            var direction = GetForcedMovementDirection(
                caster,
                target,
                effect != null ? effect.forcedMovementDirection : ForcedMovementDirectionMode.AwayFromSource);
            var destination = target.CurrentPosition + direction * distance;
            destination.y = 0f;
            return Stage01ArenaSpec.ClampPosition(destination);
        }

        private static Vector3 GetForcedMovementDirection(RuntimeHero caster, RuntimeHero target, ForcedMovementDirectionMode directionMode)
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            var sourcePosition = caster != null ? caster.CurrentPosition : target.CurrentPosition;
            var offset = directionMode == ForcedMovementDirectionMode.TowardSource
                ? sourcePosition - target.CurrentPosition
                : target.CurrentPosition - sourcePosition;
            offset.y = 0f;

            if (offset.sqrMagnitude > Mathf.Epsilon)
            {
                return offset.normalized;
            }

            return target.Side == TeamSide.Blue ? Vector3.left : Vector3.right;
        }

        private static float GetDistanceToSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            var segment = segmentEnd - segmentStart;
            var segmentLengthSquared = segment.sqrMagnitude;
            if (segmentLengthSquared <= Mathf.Epsilon)
            {
                return Vector3.Distance(point, segmentStart);
            }

            var projectedDistance = Mathf.Clamp01(Vector3.Dot(point - segmentStart, segment) / segmentLengthSquared);
            var projection = segmentStart + segment * projectedDistance;
            return Vector3.Distance(point, projection);
        }
    }
}
