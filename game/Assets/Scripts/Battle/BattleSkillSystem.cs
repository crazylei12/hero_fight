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

        private readonly struct UltimateConditionTrace
        {
            public UltimateConditionTrace(bool wasEvaluated, bool passed, string summary)
            {
                WasEvaluated = wasEvaluated;
                Passed = passed;
                Summary = summary ?? string.Empty;
            }

            public bool WasEvaluated { get; }

            public bool Passed { get; }

            public string Summary { get; }
        }

        private readonly struct UltimateDecisionTrace
        {
            public UltimateDecisionTrace(bool passed, int fallbackStage, string summary)
            {
                Passed = passed;
                FallbackStage = fallbackStage;
                Summary = summary ?? string.Empty;
            }

            public bool Passed { get; }

            public int FallbackStage { get; }

            public string Summary { get; }
        }

        private readonly struct UltimateSuppressionInfo
        {
            public UltimateSuppressionInfo(float multiplier, float timeSinceLastAllyUltimateSeconds)
            {
                Multiplier = multiplier;
                TimeSinceLastAllyUltimateSeconds = timeSinceLastAllyUltimateSeconds;
            }

            public float Multiplier { get; }

            public float TimeSinceLastAllyUltimateSeconds { get; }
        }

        private readonly struct UltimateChanceBreakdown
        {
            public UltimateChanceBreakdown(
                bool chanceEvaluated,
                float baseChance,
                float fallbackBonus,
                float extraUnitBonus,
                float secondaryPriorityBonus,
                float preSuppressionChance,
                float suppressionMultiplier,
                float finalChance,
                float timeSinceLastAllyUltimateSeconds,
                bool rollEvaluated,
                float rollValue,
                bool rollPassed)
            {
                ChanceEvaluated = chanceEvaluated;
                BaseChance = baseChance;
                FallbackBonus = fallbackBonus;
                ExtraUnitBonus = extraUnitBonus;
                SecondaryPriorityBonus = secondaryPriorityBonus;
                PreSuppressionChance = preSuppressionChance;
                SuppressionMultiplier = suppressionMultiplier;
                FinalChance = finalChance;
                TimeSinceLastAllyUltimateSeconds = timeSinceLastAllyUltimateSeconds;
                RollEvaluated = rollEvaluated;
                RollValue = rollValue;
                RollPassed = rollPassed;
            }

            public static UltimateChanceBreakdown NotEvaluated => new UltimateChanceBreakdown(
                chanceEvaluated: false,
                baseChance: 0f,
                fallbackBonus: 0f,
                extraUnitBonus: 0f,
                secondaryPriorityBonus: 0f,
                preSuppressionChance: 0f,
                suppressionMultiplier: 1f,
                finalChance: 0f,
                timeSinceLastAllyUltimateSeconds: -1f,
                rollEvaluated: false,
                rollValue: 0f,
                rollPassed: false);

            public bool ChanceEvaluated { get; }

            public float BaseChance { get; }

            public float FallbackBonus { get; }

            public float ExtraUnitBonus { get; }

            public float SecondaryPriorityBonus { get; }

            public float PreSuppressionChance { get; }

            public float SuppressionMultiplier { get; }

            public float FinalChance { get; }

            public float TimeSinceLastAllyUltimateSeconds { get; }

            public bool RollEvaluated { get; }

            public float RollValue { get; }

            public bool RollPassed { get; }

            public UltimateChanceBreakdown WithRoll(bool rollEvaluated, float rollValue, bool rollPassed)
            {
                return new UltimateChanceBreakdown(
                    ChanceEvaluated,
                    BaseChance,
                    FallbackBonus,
                    ExtraUnitBonus,
                    SecondaryPriorityBonus,
                    PreSuppressionChance,
                    SuppressionMultiplier,
                    FinalChance,
                    TimeSinceLastAllyUltimateSeconds,
                    rollEvaluated,
                    rollValue,
                    rollPassed);
            }
        }

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
            if (skill == null || skill.activationMode != SkillActivationMode.Active)
            {
                return false;
            }

            return TryCastSpecificSkill(context, caster, skill, battleManager, requireHighValueCast: false);
        }

        private static bool TryCastUltimate(BattleContext context, RuntimeHero caster, SkillData skill, BattleManager battleManager)
        {
            if (skill == null)
            {
                return false;
            }

            if (skill.activationMode != SkillActivationMode.Active
                || skill.slotType != SkillSlotType.Ultimate
                || !caster.CanUseUltimate())
            {
                return false;
            }

            if (!ShouldAttemptUltimateCast(context, caster))
            {
                return false;
            }

            var usesTemplateDecision = UsesUltimateDecisionTemplate(skill);
            var chanceBreakdown = UltimateChanceBreakdown.NotEvaluated;
            var primaryTarget = usesTemplateDecision
                ? SelectUltimatePrimaryTarget(context, caster, skill)
                : SelectPrimaryTarget(context, caster, skill);
            if (!IsPrimaryTargetStillValid(skill, caster, primaryTarget))
            {
                ScheduleNextUltimateAttempt(context, caster);
                PublishUltimateDecisionEvaluated(
                    context,
                    caster,
                    skill,
                    primaryTarget,
                    0,
                    usesTemplateDecision,
                    0,
                    UltimateDecisionOutcome.InvalidPrimaryTarget,
                    $"primaryTarget={FormatUltimateTargetLabel(primaryTarget)} valid=false allowsMissing={AllowsMissingPrimaryTarget(skill)}",
                    chanceBreakdown);
                return false;
            }

            if (primaryTarget == null && !skill.allowsSelfCast && !AllowsMissingPrimaryTarget(skill))
            {
                ScheduleNextUltimateAttempt(context, caster);
                PublishUltimateDecisionEvaluated(
                    context,
                    caster,
                    skill,
                    primaryTarget,
                    0,
                    usesTemplateDecision,
                    0,
                    UltimateDecisionOutcome.MissingPrimaryTarget,
                    "primaryTarget=none requiresDirectTarget=true",
                    chanceBreakdown);
                return false;
            }

            var affectedTargets = CollectTargets(context, caster, skill, primaryTarget);
            if (usesTemplateDecision)
            {
                if (affectedTargets.Count <= 0)
                {
                    ScheduleNextUltimateAttempt(context, caster);
                    PublishUltimateDecisionEvaluated(
                        context,
                        caster,
                        skill,
                        primaryTarget,
                        affectedTargets.Count,
                        usesTemplateDecision,
                        0,
                        UltimateDecisionOutcome.NoAffectedTargets,
                        "affectedTargets=0 templateGate=requiresAtLeastOne",
                        chanceBreakdown);
                    return false;
                }
            }
            else if (affectedTargets.Count < Mathf.Max(1, skill.minTargetsToCast))
            {
                ScheduleNextUltimateAttempt(context, caster);
                PublishUltimateDecisionEvaluated(
                    context,
                    caster,
                    skill,
                    primaryTarget,
                    affectedTargets.Count,
                    usesTemplateDecision,
                    0,
                    UltimateDecisionOutcome.InsufficientAffectedTargets,
                    $"affectedTargets={affectedTargets.Count} minTargets={Mathf.Max(1, skill.minTargetsToCast)}",
                    chanceBreakdown);
                return false;
            }

            if (usesTemplateDecision)
            {
                var decisionTrace = EvaluateUltimateDecisionTrace(context, caster, skill, primaryTarget, affectedTargets);
                if (!decisionTrace.Passed)
                {
                    ScheduleNextUltimateAttempt(context, caster);
                    PublishUltimateDecisionEvaluated(
                        context,
                        caster,
                        skill,
                        primaryTarget,
                        affectedTargets.Count,
                        usesTemplateDecision,
                        decisionTrace.FallbackStage,
                        UltimateDecisionOutcome.DecisionConditionsFailed,
                        decisionTrace.Summary,
                        chanceBreakdown);
                    return false;
                }

                chanceBreakdown = RollUltimateCastChance(context, caster, skill, primaryTarget);
                PublishUltimateDecisionEvaluated(
                    context,
                    caster,
                    skill,
                    primaryTarget,
                    affectedTargets.Count,
                    usesTemplateDecision,
                    decisionTrace.FallbackStage,
                    chanceBreakdown.RollPassed ? UltimateDecisionOutcome.RollPassed : UltimateDecisionOutcome.RollFailed,
                    decisionTrace.Summary,
                    chanceBreakdown);

                if (!chanceBreakdown.RollPassed)
                {
                    return false;
                }
            }
            else
            {
                var hasHighValueOpportunity = HasHighValueOpportunity(skill, affectedTargets);
                var legacySummary = BuildLegacyUltimateDecisionSummary(skill, affectedTargets, hasHighValueOpportunity);
                if (!hasHighValueOpportunity)
                {
                    ScheduleNextUltimateAttempt(context, caster);
                    PublishUltimateDecisionEvaluated(
                        context,
                        caster,
                        skill,
                        primaryTarget,
                        affectedTargets.Count,
                        usesTemplateDecision,
                        0,
                        UltimateDecisionOutcome.LegacyOpportunityFailed,
                        legacySummary,
                        chanceBreakdown);
                    return false;
                }

                chanceBreakdown = RollLegacyUltimateCastChance(context, caster, skill, affectedTargets);
                PublishUltimateDecisionEvaluated(
                    context,
                    caster,
                    skill,
                    primaryTarget,
                    affectedTargets.Count,
                    usesTemplateDecision,
                    0,
                    chanceBreakdown.RollPassed ? UltimateDecisionOutcome.RollPassed : UltimateDecisionOutcome.RollFailed,
                    legacySummary,
                    chanceBreakdown);

                if (!chanceBreakdown.RollPassed)
                {
                    return false;
                }
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

            if (skill.activationMode != SkillActivationMode.Active)
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
            return SelectPrimaryTargetByType(context, caster, skill, skill.targetType, effectiveCastRange, allowFallbackForPriorityTarget: true);
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

            return SelectPrimaryTargetByType(context, caster, skill, skill.targetType, effectiveCastRange, allowFallbackForPriorityTarget: true);
        }

        private static RuntimeHero SelectPrimaryTargetByType(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            SkillTargetType targetType,
            float effectiveCastRange,
            bool allowFallbackForPriorityTarget)
        {
            if (context == null || caster == null || skill == null)
            {
                return null;
            }

            switch (targetType)
            {
                case SkillTargetType.Self:
                    return caster;
                case SkillTargetType.AllAllies:
                    return FindFirstGlobalTeamTarget(context.Heroes, caster, skill, includeAllies: true);
                case SkillTargetType.AllEnemies:
                    return FindFirstGlobalTeamTarget(context.Heroes, caster, skill, includeAllies: false);
                case SkillTargetType.NearestEnemy:
                    return SelectCurrentOrNearestEnemyTarget(context, caster, skill, effectiveCastRange);
                case SkillTargetType.CurrentEnemyTarget:
                    return SelectCurrentEnemyTarget(context, caster, skill, effectiveCastRange);
                case SkillTargetType.LowestHealthEnemy:
                    return FindLowestHealth(context.Heroes, caster, skill, includeAllies: false, effectiveCastRange);
                case SkillTargetType.LowestHealthAlly:
                    return FindLowestHealth(context.Heroes, caster, skill, includeAllies: true, effectiveCastRange);
                case SkillTargetType.LowestHealthRangedAlly:
                    return BattleAiDirector.SelectLowestHealthRangedAllyTarget(context.Heroes, caster, effectiveCastRange, allowHealthyFallback: false);
                case SkillTargetType.ThreatenedRangedAlly:
                    var threatenedAlly = BattleAiDirector.SelectThreatenedRangedAllyTarget(
                        context.Heroes,
                        caster,
                        effectiveCastRange,
                        GetPrioritySearchRadius(skill),
                        GetPriorityRequiredUnitCount(skill));
                    if (threatenedAlly != null || !allowFallbackForPriorityTarget)
                    {
                        return threatenedAlly;
                    }

                    return skill.fallbackTargetType == SkillTargetType.None
                        ? null
                        : SelectPrimaryTargetByType(
                            context,
                            caster,
                            skill,
                            skill.fallbackTargetType,
                            effectiveCastRange,
                            allowFallbackForPriorityTarget: false);
                case SkillTargetType.ThreatenedAlly:
                    var threatenedAnyAlly = BattleAiDirector.SelectThreatenedAllyTarget(
                        context.Heroes,
                        caster,
                        effectiveCastRange,
                        GetPrioritySearchRadius(skill),
                        GetPriorityRequiredUnitCount(skill));
                    if (threatenedAnyAlly != null || !allowFallbackForPriorityTarget)
                    {
                        return threatenedAnyAlly;
                    }

                    return skill.fallbackTargetType == SkillTargetType.None
                        ? null
                        : SelectPrimaryTargetByType(
                            context,
                            caster,
                            skill,
                            skill.fallbackTargetType,
                            effectiveCastRange,
                            allowFallbackForPriorityTarget: false);
                case SkillTargetType.ThreatenedRangedAllyOrEnemyDensestAnchor:
                    return SelectThreatenedRangedAllyOrEnemyAnchor(context, caster, skill, effectiveCastRange);
                case SkillTargetType.HighestDamageEnemyInRange:
                    return BattleAiDirector.SelectHighestDamageEnemyTarget(context.Heroes, caster, effectiveCastRange);
                case SkillTargetType.DensestEnemyArea:
                    return FindDensestEnemyAnchor(context.Heroes, caster, effectiveCastRange, skill.areaRadius);
                case SkillTargetType.BackmostEnemy:
                    return BattleAiDirector.SelectBackmostEnemyTarget(context.Heroes, caster, effectiveCastRange);
                case SkillTargetType.PriorityEnemyHeroClass:
                    var preferredTarget = BattleAiDirector.SelectEnemyTargetByHeroClass(
                        context.Heroes,
                        caster,
                        effectiveCastRange,
                        skill.preferredEnemyHeroClass);
                    if (preferredTarget != null || !allowFallbackForPriorityTarget)
                    {
                        return preferredTarget;
                    }

                    var fallbackTargetType = skill.fallbackTargetType == SkillTargetType.PriorityEnemyHeroClass
                        ? SkillTargetType.NearestEnemy
                        : skill.fallbackTargetType;
                    return fallbackTargetType == SkillTargetType.None
                        ? null
                        : SelectPrimaryTargetByType(
                            context,
                            caster,
                            skill,
                            fallbackTargetType,
                            effectiveCastRange,
                            allowFallbackForPriorityTarget: false);
                default:
                    return null;
            }
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

        private static RuntimeHero SelectCurrentEnemyTarget(
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
            return currentTarget != null
                && IsValidTargetForSkill(skill, caster, currentTarget)
                && IsPrimaryTargetStillValidForCastRange(skill, caster, currentTarget, effectiveCastRange)
                ? currentTarget
                : null;
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
                case SkillTargetType.LowestHealthRangedAlly:
                case SkillTargetType.ThreatenedRangedAlly:
                case SkillTargetType.AllAllies:
                    return candidate.Side == caster.Side
                        && (skill.targetType != SkillTargetType.LowestHealthRangedAlly
                            && skill.targetType != SkillTargetType.ThreatenedRangedAlly
                            || IsRangedAlly(candidate, caster));
                case SkillTargetType.ThreatenedAlly:
                    return candidate.Side == caster.Side && candidate != caster;
                case SkillTargetType.AllEnemies:
                case SkillTargetType.CurrentEnemyTarget:
                    return candidate.Side != caster.Side;
                case SkillTargetType.ThreatenedRangedAllyOrEnemyDensestAnchor:
                    return true;
                case SkillTargetType.PriorityEnemyHeroClass:
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

        private static UltimateDecisionTrace EvaluateUltimateDecisionTrace(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            List<RuntimeHero> affectedTargets)
        {
            var decision = skill?.ultimateDecision;
            var fallbackStage = GetActiveFallbackStage(context, decision?.fallback);
            if (decision == null || decision.primaryCondition == null)
            {
                return new UltimateDecisionTrace(false, fallbackStage, "templateDecision=missing-config");
            }

            var primaryTrace = EvaluateUltimateConditionTrace(
                context,
                caster,
                skill,
                primaryTarget,
                decision.primaryCondition,
                decision.fallback,
                affectedTargets,
                "primary");
            var hasSecondaryCondition = decision.secondaryCondition != null
                && decision.secondaryCondition.conditionType != UltimateConditionType.None;
            var secondaryTrace = hasSecondaryCondition
                ? EvaluateUltimateConditionTrace(context, caster, skill, primaryTarget, decision.secondaryCondition, decision.fallback, affectedTargets, "secondary")
                : new UltimateConditionTrace(false, true, "secondary=none");

            var finalPass = primaryTrace.Passed;
            if (decision.combineMode != UltimateConditionCombineMode.PrimaryOnly && hasSecondaryCondition)
            {
                finalPass = decision.combineMode switch
                {
                    UltimateConditionCombineMode.AllMustPass => primaryTrace.Passed && secondaryTrace.Passed,
                    UltimateConditionCombineMode.AnyPass => primaryTrace.Passed || secondaryTrace.Passed,
                    _ => primaryTrace.Passed,
                };
            }

            var secondarySuffix = decision.combineMode == UltimateConditionCombineMode.PrimaryOnly && hasSecondaryCondition
                ? " bonusOnly=true"
                : string.Empty;
            var summary = $"combine={decision.combineMode} fallbackStage={fallbackStage}; {primaryTrace.Summary}; {secondaryTrace.Summary}{secondarySuffix}; finalPass={finalPass}";
            return new UltimateDecisionTrace(finalPass, fallbackStage, summary);
        }

        private static UltimateConditionTrace EvaluateUltimateConditionTrace(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            UltimateConditionData condition,
            UltimateFallbackData fallback,
            List<RuntimeHero> affectedTargets,
            string label)
        {
            if (condition == null || condition.conditionType == UltimateConditionType.None)
            {
                return new UltimateConditionTrace(false, true, $"{label}=none");
            }

            var pass = EvaluateUltimateCondition(context, caster, skill, primaryTarget, condition, fallback, affectedTargets);
            var searchRadius = GetEffectiveSearchRadius(skill, condition);
            var requiredUnitCount = GetEffectiveRequiredUnitCount(condition, fallback, context);
            var healthThreshold = GetEffectiveHealthPercentThreshold(condition, fallback, context);
            string summary;

            switch (condition.conditionType)
            {
                case UltimateConditionType.EnemyCountInRange:
                    if (ShouldUseThreatenedRangedAnchorCount(skill, caster, primaryTarget))
                    {
                        var priorityMeasuredCount = CountUnitsInRange(
                            context,
                            caster,
                            primaryTarget,
                            GetPrioritySearchRadius(skill),
                            countAllies: false);
                        summary = $"{label}={condition.conditionType} measured={priorityMeasuredCount} required={GetPriorityRequiredUnitCount(skill)} radius={GetPrioritySearchRadius(skill):0.##} anchor=threatened-ranged pass={pass}";
                        break;
                    }

                    var enemyCount = CountUnitsInRange(context, caster, primaryTarget, searchRadius, countAllies: false);
                    summary = $"{label}={condition.conditionType} measured={enemyCount} required={requiredUnitCount} radius={searchRadius:0.##} pass={pass}";
                    break;
                case UltimateConditionType.AllyCountInRange:
                    var allyCount = CountUnitsInRange(context, caster, primaryTarget, searchRadius, countAllies: true);
                    summary = $"{label}={condition.conditionType} measured={allyCount} required={requiredUnitCount} radius={searchRadius:0.##} pass={pass}";
                    break;
                case UltimateConditionType.EnemyLowHealthInRange:
                    var lowHealthEnemyCount = CountLowHealthUnitsInRange(context, caster, primaryTarget, searchRadius, includeAllies: false, healthThreshold);
                    summary = $"{label}={condition.conditionType} measured={lowHealthEnemyCount} required={requiredUnitCount} radius={searchRadius:0.##} threshold={healthThreshold:0.00} pass={pass}";
                    break;
                case UltimateConditionType.EnemyWithStatusInRange:
                    var statusEnemyCount = CountUnitsWithStatusInRange(context, caster, primaryTarget, searchRadius, condition);
                    summary = $"{label}={condition.conditionType} measured={statusEnemyCount} required={requiredUnitCount} radius={searchRadius:0.##} status={condition.statusEffectTypeFilter} theme={FormatStatusThemeLabel(condition.statusThemeKey)} minStacks={Mathf.Max(1, condition.minimumStatusStacks)} pass={pass}";
                    break;
                case UltimateConditionType.EnemyLowHealthWithStatusInRange:
                    var lowHealthStatusEnemyCount = CountLowHealthUnitsWithStatusInRange(context, caster, primaryTarget, searchRadius, healthThreshold, condition);
                    summary = $"{label}={condition.conditionType} measured={lowHealthStatusEnemyCount} required={requiredUnitCount} radius={searchRadius:0.##} threshold={healthThreshold:0.00} status={condition.statusEffectTypeFilter} theme={FormatStatusThemeLabel(condition.statusThemeKey)} minStacks={Mathf.Max(1, condition.minimumStatusStacks)} pass={pass}";
                    break;
                case UltimateConditionType.AllyLowHealthInRange:
                    var lowHealthAllyCount = CountLowHealthUnitsInRange(context, caster, primaryTarget, searchRadius, includeAllies: true, healthThreshold);
                    summary = $"{label}={condition.conditionType} measured={lowHealthAllyCount} required={requiredUnitCount} radius={searchRadius:0.##} threshold={healthThreshold:0.00} pass={pass}";
                    break;
                case UltimateConditionType.EnemyCountInDashPath:
                    var dashPathTargetCount = CountDashPathTargets(context, caster, skill, primaryTarget);
                    summary = $"{label}={condition.conditionType} measured={dashPathTargetCount} required={requiredUnitCount} pass={pass}";
                    break;
                case UltimateConditionType.SelfLowHealth:
                    summary = $"{label}={condition.conditionType} currentHpRatio={GetHealthRatio(caster):0.00} threshold={healthThreshold:0.00} pass={pass}";
                    break;
                case UltimateConditionType.EnemyHeroClassInRange:
                    var heroClassCount = CountUnitsOfHeroClassInRange(context, caster, primaryTarget, searchRadius, condition.heroClassFilter);
                    summary = $"{label}={condition.conditionType} class={condition.heroClassFilter} measured={heroClassCount} required={requiredUnitCount} radius={searchRadius:0.##} pass={pass}";
                    break;
                case UltimateConditionType.TargetIsHighValue:
                    var distanceToTarget = primaryTarget != null
                        ? Vector3.Distance(caster.CurrentPosition, primaryTarget.CurrentPosition)
                        : -1f;
                    var distanceLabel = distanceToTarget >= 0f ? distanceToTarget.ToString("0.##") : "none";
                    summary = $"{label}={condition.conditionType} type={condition.highValueTargetType} requireInRange={condition.requireTargetInCastRange} distance={distanceLabel} pass={pass}";
                    break;
                case UltimateConditionType.InCombatDuration:
                    summary = $"{label}={condition.conditionType} engaged={caster.CombatEngagedSeconds:0.00} required={condition.durationSeconds:0.00} pass={pass}";
                    break;
                default:
                    summary = $"{label}={condition.conditionType} affected={(affectedTargets != null ? affectedTargets.Count : 0)} pass={pass}";
                    break;
            }

            return new UltimateConditionTrace(true, pass, summary);
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
                    if (ShouldUseThreatenedRangedAnchorCount(skill, caster, primaryTarget))
                    {
                        return CountUnitsInRange(
                                   context,
                                   caster,
                                   primaryTarget,
                                   GetPrioritySearchRadius(skill),
                                   countAllies: false) >= GetPriorityRequiredUnitCount(skill);
                    }

                    return CountUnitsInRange(context, caster, primaryTarget, searchRadius, countAllies: false) >= requiredUnitCount;
                case UltimateConditionType.AllyCountInRange:
                    return CountUnitsInRange(context, caster, primaryTarget, searchRadius, countAllies: true) >= requiredUnitCount;
                case UltimateConditionType.EnemyLowHealthInRange:
                    return CountLowHealthUnitsInRange(context, caster, primaryTarget, searchRadius, includeAllies: false, healthThreshold) >= requiredUnitCount;
                case UltimateConditionType.EnemyWithStatusInRange:
                    return CountUnitsWithStatusInRange(context, caster, primaryTarget, searchRadius, condition) >= requiredUnitCount;
                case UltimateConditionType.EnemyLowHealthWithStatusInRange:
                    return CountLowHealthUnitsWithStatusInRange(context, caster, primaryTarget, searchRadius, healthThreshold, condition) >= requiredUnitCount;
                case UltimateConditionType.AllyLowHealthInRange:
                    return CountLowHealthUnitsInRange(context, caster, primaryTarget, searchRadius, includeAllies: true, healthThreshold) >= requiredUnitCount;
                case UltimateConditionType.EnemyCountInDashPath:
                    return CountDashPathTargets(context, caster, skill, primaryTarget) >= requiredUnitCount;
                case UltimateConditionType.SelfLowHealth:
                    return GetHealthRatio(caster) <= healthThreshold;
                case UltimateConditionType.EnemyHeroClassInRange:
                    return CountUnitsOfHeroClassInRange(context, caster, primaryTarget, searchRadius, condition.heroClassFilter) >= requiredUnitCount;
                case UltimateConditionType.TargetIsHighValue:
                    return IsHighValueTarget(primaryTarget, caster, skill, condition);
                case UltimateConditionType.InCombatDuration:
                    return caster.CombatEngagedSeconds >= condition.durationSeconds;
                default:
                    return affectedTargets != null && affectedTargets.Count > 0;
            }
        }

        private static UltimateChanceBreakdown RollUltimateCastChance(BattleContext context, RuntimeHero caster, SkillData skill, RuntimeHero primaryTarget)
        {
            var chanceBreakdown = GetUltimateCastChanceBreakdown(context, caster, skill, primaryTarget);
            ScheduleNextUltimateAttempt(context, caster);
            return ResolveUltimateChanceRoll(context, chanceBreakdown);
        }

        private static UltimateChanceBreakdown RollLegacyUltimateCastChance(BattleContext context, RuntimeHero caster, SkillData skill, List<RuntimeHero> affectedTargets)
        {
            var minimumTargets = Mathf.Max(1, skill != null ? skill.minTargetsToCast : 1);
            var extraUnitBonus = Mathf.Max(0, (affectedTargets != null ? affectedTargets.Count : 0) - minimumTargets) * UltimateExtraUnitReleaseChance;
            var chanceBreakdown = CreateUltimateChanceBreakdown(
                context,
                caster,
                UltimateBaseReleaseChance,
                0f,
                extraUnitBonus,
                0f);
            ScheduleNextUltimateAttempt(context, caster);
            return ResolveUltimateChanceRoll(context, chanceBreakdown);
        }

        private static UltimateChanceBreakdown GetUltimateCastChanceBreakdown(BattleContext context, RuntimeHero caster, SkillData skill, RuntimeHero primaryTarget)
        {
            var decision = skill?.ultimateDecision;
            var primaryCondition = decision?.primaryCondition;
            if (primaryCondition == null)
            {
                return CreateUltimateChanceBreakdown(
                    context,
                    caster,
                    baseChance: 1f,
                    fallbackBonus: 0f,
                    extraUnitBonus: 0f,
                    secondaryPriorityBonus: 0f);
            }

            var fallbackBonus = 0f;
            var extraUnitBonus = 0f;
            var secondaryPriorityBonus = 0f;
            var fallbackStage = GetActiveFallbackStage(context, decision?.fallback);
            if (fallbackStage >= 1)
            {
                fallbackBonus += UltimateFirstFallbackBonus;
            }

            if (fallbackStage >= 2)
            {
                fallbackBonus += UltimateSecondFallbackBonus;
            }

            switch (primaryCondition.conditionType)
            {
                case UltimateConditionType.EnemyCountInRange:
                case UltimateConditionType.AllyCountInRange:
                case UltimateConditionType.EnemyLowHealthInRange:
                case UltimateConditionType.EnemyWithStatusInRange:
                case UltimateConditionType.EnemyLowHealthWithStatusInRange:
                case UltimateConditionType.AllyLowHealthInRange:
                case UltimateConditionType.EnemyHeroClassInRange:
                case UltimateConditionType.EnemyCountInDashPath:
                    var measuredCount = GetConditionUnitCount(context, caster, skill, primaryTarget, primaryCondition, decision?.fallback);
                    var requiredCount = GetEffectiveRequiredUnitCount(primaryCondition, decision?.fallback, context);
                    extraUnitBonus = Mathf.Max(0, measuredCount - requiredCount) * UltimateExtraUnitReleaseChance;
                    break;
                case UltimateConditionType.SelfLowHealth:
                    extraUnitBonus = Mathf.Clamp01((primaryCondition.healthPercentThreshold - GetHealthRatio(caster)) * 1.5f);
                    break;
                case UltimateConditionType.InCombatDuration:
                    extraUnitBonus = Mathf.Clamp01((caster.CombatEngagedSeconds - primaryCondition.durationSeconds) * 0.1f);
                    break;
                case UltimateConditionType.TargetIsHighValue:
                    extraUnitBonus = 0.1f;
                    break;
            }

            var secondaryCondition = decision?.secondaryCondition;
            if (decision != null
                && decision.combineMode == UltimateConditionCombineMode.PrimaryOnly
                && secondaryCondition != null
                && secondaryCondition.conditionType != UltimateConditionType.None
                && EvaluateUltimateCondition(context, caster, skill, primaryTarget, secondaryCondition, decision.fallback, null))
            {
                // Stage-01 uses PrimaryOnly + secondaryCondition to express
                // "the main cast gate is unchanged, but this opportunity is more attractive".
                secondaryPriorityBonus = UltimateSecondaryPriorityBonus;
            }

            return CreateUltimateChanceBreakdown(
                context,
                caster,
                UltimateBaseReleaseChance,
                fallbackBonus,
                extraUnitBonus,
                secondaryPriorityBonus);
        }

        private static UltimateChanceBreakdown CreateUltimateChanceBreakdown(
            BattleContext context,
            RuntimeHero caster,
            float baseChance,
            float fallbackBonus,
            float extraUnitBonus,
            float secondaryPriorityBonus)
        {
            var preSuppressionChance = Mathf.Clamp01(baseChance + fallbackBonus + extraUnitBonus + secondaryPriorityBonus);
            var suppressionInfo = GetUltimateSuppressionInfo(context, caster);
            var finalChance = Mathf.Clamp01(preSuppressionChance * suppressionInfo.Multiplier);
            return new UltimateChanceBreakdown(
                true,
                baseChance,
                fallbackBonus,
                extraUnitBonus,
                secondaryPriorityBonus,
                preSuppressionChance,
                suppressionInfo.Multiplier,
                finalChance,
                suppressionInfo.TimeSinceLastAllyUltimateSeconds,
                false,
                0f,
                false);
        }

        private static UltimateChanceBreakdown ResolveUltimateChanceRoll(BattleContext context, UltimateChanceBreakdown chanceBreakdown)
        {
            if (!chanceBreakdown.ChanceEvaluated)
            {
                return chanceBreakdown;
            }

            if (context?.RandomService == null)
            {
                return chanceBreakdown.WithRoll(false, 0f, true);
            }

            var rollValue = context.RandomService.NextFloat();
            return chanceBreakdown.WithRoll(
                true,
                rollValue,
                rollValue <= chanceBreakdown.FinalChance);
        }

        private static UltimateSuppressionInfo GetUltimateSuppressionInfo(BattleContext context, RuntimeHero caster)
        {
            if (context?.Clock == null || caster == null || caster.Side == TeamSide.None)
            {
                return new UltimateSuppressionInfo(multiplier: 1f, timeSinceLastAllyUltimateSeconds: -1f);
            }

            var lastAllyUltimateCastTimeSeconds = context.GetLastUltimateCastTimeSeconds(caster.Side);
            if (float.IsNegativeInfinity(lastAllyUltimateCastTimeSeconds))
            {
                return new UltimateSuppressionInfo(multiplier: 1f, timeSinceLastAllyUltimateSeconds: -1f);
            }

            var timeSinceLastAllyUltimateSeconds = Mathf.Max(0f, context.Clock.ElapsedTimeSeconds - lastAllyUltimateCastTimeSeconds);
            if (timeSinceLastAllyUltimateSeconds > UltimateAllySuppressionWindowSeconds)
            {
                return new UltimateSuppressionInfo(multiplier: 1f, timeSinceLastAllyUltimateSeconds);
            }

            return new UltimateSuppressionInfo(
                multiplier: UltimateAllySuppressionChanceMultiplier,
                timeSinceLastAllyUltimateSeconds);
        }

        private static void PublishUltimateDecisionEvaluated(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            int affectedTargetCount,
            bool usesTemplateDecision,
            int fallbackStage,
            UltimateDecisionOutcome outcome,
            string decisionSummary,
            UltimateChanceBreakdown chanceBreakdown)
        {
            if (context?.EventBus == null || caster == null || skill == null)
            {
                return;
            }

            context.EventBus.Publish(new UltimateDecisionEvaluatedEvent(
                caster,
                skill,
                primaryTarget,
                usesTemplateDecision,
                affectedTargetCount,
                fallbackStage,
                outcome,
                decisionSummary,
                chanceBreakdown.ChanceEvaluated,
                BuildUltimateChanceSummary(chanceBreakdown),
                chanceBreakdown.FinalChance,
                chanceBreakdown.SuppressionMultiplier,
                chanceBreakdown.TimeSinceLastAllyUltimateSeconds,
                chanceBreakdown.RollEvaluated,
                chanceBreakdown.RollValue,
                chanceBreakdown.RollPassed,
                caster.NextUltimateDecisionCheckTimeSeconds));
        }

        private static string BuildLegacyUltimateDecisionSummary(SkillData skill, List<RuntimeHero> affectedTargets, bool hasHighValueOpportunity)
        {
            return $"legacy skillType={skill?.skillType.ToString() ?? "Unknown"} affected={(affectedTargets != null ? affectedTargets.Count : 0)} minTargets={Mathf.Max(1, skill != null ? skill.minTargetsToCast : 1)} highValueRule={GetLegacyHighValueRuleLabel(skill)} highValuePass={hasHighValueOpportunity}";
        }

        private static string GetLegacyHighValueRuleLabel(SkillData skill)
        {
            if (skill == null)
            {
                return "unknown";
            }

            return skill.skillType switch
            {
                SkillType.AreaDamage => "atLeast2Targets",
                SkillType.Stun => "atLeast2Targets",
                SkillType.KnockUp => "atLeast2Targets",
                SkillType.SingleTargetHeal => "anyValidTarget",
                SkillType.AreaHeal => "anyValidTarget",
                SkillType.Buff => "anyValidTarget",
                _ => "anyValidTarget",
            };
        }

        private static string BuildUltimateChanceSummary(UltimateChanceBreakdown chanceBreakdown)
        {
            if (!chanceBreakdown.ChanceEvaluated)
            {
                return "chance=skipped";
            }

            var allyGapLabel = chanceBreakdown.TimeSinceLastAllyUltimateSeconds >= 0f
                ? chanceBreakdown.TimeSinceLastAllyUltimateSeconds.ToString("0.00")
                : "none";
            return $"chance={{base={chanceBreakdown.BaseChance:0.00} fallback={chanceBreakdown.FallbackBonus:0.00} extra={chanceBreakdown.ExtraUnitBonus:0.00} secondary={chanceBreakdown.SecondaryPriorityBonus:0.00} pre={chanceBreakdown.PreSuppressionChance:0.00} suppression={chanceBreakdown.SuppressionMultiplier:0.00} allyGap={allyGapLabel} final={chanceBreakdown.FinalChance:0.00}}}";
        }

        private static string FormatUltimateTargetLabel(RuntimeHero target)
        {
            if (target == null)
            {
                return "none";
            }

            var displayName = target.Definition != null && !string.IsNullOrWhiteSpace(target.Definition.displayName)
                ? target.Definition.displayName
                : target.RuntimeId;
            return $"{displayName}[{target.Side}|{target.RuntimeId}]";
        }

        private static int GetConditionUnitCount(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            UltimateConditionData condition,
            UltimateFallbackData fallback)
        {
            var searchRadius = GetEffectiveSearchRadius(skill, condition);
            var healthThreshold = GetEffectiveHealthPercentThreshold(condition, fallback, context);

            return condition.conditionType switch
            {
                UltimateConditionType.EnemyCountInRange => ShouldUseThreatenedRangedAnchorCount(skill, caster, primaryTarget)
                    ? CountUnitsInRange(context, caster, primaryTarget, GetPrioritySearchRadius(skill), countAllies: false)
                    : CountUnitsInRange(context, caster, primaryTarget, searchRadius, countAllies: false),
                UltimateConditionType.AllyCountInRange => CountUnitsInRange(context, caster, primaryTarget, searchRadius, countAllies: true),
                UltimateConditionType.EnemyLowHealthInRange => CountLowHealthUnitsInRange(context, caster, primaryTarget, searchRadius, includeAllies: false, healthThreshold),
                UltimateConditionType.EnemyWithStatusInRange => CountUnitsWithStatusInRange(context, caster, primaryTarget, searchRadius, condition),
                UltimateConditionType.EnemyLowHealthWithStatusInRange => CountLowHealthUnitsWithStatusInRange(context, caster, primaryTarget, searchRadius, healthThreshold, condition),
                UltimateConditionType.AllyLowHealthInRange => CountLowHealthUnitsInRange(context, caster, primaryTarget, searchRadius, includeAllies: true, healthThreshold),
                UltimateConditionType.EnemyHeroClassInRange => CountUnitsOfHeroClassInRange(context, caster, primaryTarget, searchRadius, condition.heroClassFilter),
                UltimateConditionType.EnemyCountInDashPath => CountDashPathTargets(context, caster, skill, primaryTarget),
                _ => 0,
            };
        }

        private static int CountDashPathTargets(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget)
        {
            if (context == null
                || caster == null
                || primaryTarget == null
                || primaryTarget.IsDead
                || skill?.effects == null)
            {
                return 0;
            }

            var resolutionState = CreateSkillEffectResolutionState(skill, caster, primaryTarget);
            if (!resolutionState.HasDashPath)
            {
                return 0;
            }

            var uniqueTargetIds = new HashSet<string>();
            for (var i = 0; i < skill.effects.Count; i++)
            {
                var effect = skill.effects[i];
                if (effect == null || effect.targetMode != SkillEffectTargetMode.DashPathEnemies)
                {
                    continue;
                }

                var dashTargets = CollectDashPathTargets(context, caster, skill, effect, resolutionState);
                for (var j = 0; j < dashTargets.Count; j++)
                {
                    var target = dashTargets[j];
                    if (target != null)
                    {
                        uniqueTargetIds.Add(target.RuntimeId);
                    }
                }
            }

            return uniqueTargetIds.Count;
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

        private static bool ShouldUseThreatenedRangedAnchorCount(SkillData skill, RuntimeHero caster, RuntimeHero primaryTarget)
        {
            return skill != null
                && caster != null
                && primaryTarget != null
                && skill.targetType == SkillTargetType.ThreatenedRangedAllyOrEnemyDensestAnchor
                && primaryTarget.Side == caster.Side
                && IsRangedAlly(primaryTarget, caster);
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

        private static int CountUnitsWithStatusInRange(
            BattleContext context,
            RuntimeHero caster,
            RuntimeHero primaryTarget,
            float searchRadius,
            UltimateConditionData condition)
        {
            return CountUnitsMatchingStatusCondition(
                context,
                caster,
                primaryTarget,
                searchRadius,
                condition,
                requireLowHealth: false,
                healthPercentThreshold: 1f);
        }

        private static int CountLowHealthUnitsWithStatusInRange(
            BattleContext context,
            RuntimeHero caster,
            RuntimeHero primaryTarget,
            float searchRadius,
            float healthPercentThreshold,
            UltimateConditionData condition)
        {
            return CountUnitsMatchingStatusCondition(
                context,
                caster,
                primaryTarget,
                searchRadius,
                condition,
                requireLowHealth: true,
                healthPercentThreshold);
        }

        private static int CountUnitsMatchingStatusCondition(
            BattleContext context,
            RuntimeHero caster,
            RuntimeHero primaryTarget,
            float searchRadius,
            UltimateConditionData condition,
            bool requireLowHealth,
            float healthPercentThreshold)
        {
            if (context?.Heroes == null
                || caster == null
                || condition == null
                || condition.statusEffectTypeFilter == StatusEffectType.None)
            {
                return 0;
            }

            var center = GetSearchCenter(caster, primaryTarget);
            var count = 0;
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate == null || candidate.IsDead || candidate.Side == caster.Side)
                {
                    continue;
                }

                if (Vector3.Distance(candidate.CurrentPosition, center) > searchRadius)
                {
                    continue;
                }

                if (requireLowHealth && GetHealthRatio(candidate) > healthPercentThreshold)
                {
                    continue;
                }

                if (StatusEffectSystem.GetStatusStackCount(
                        candidate,
                        condition.statusEffectTypeFilter,
                        condition.statusThemeKey) < Mathf.Max(1, condition.minimumStatusStacks))
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static int CountUnitsOfHeroClassInRange(
            BattleContext context,
            RuntimeHero caster,
            RuntimeHero primaryTarget,
            float searchRadius,
            HeroClass heroClassFilter)
        {
            var center = GetSearchCenter(caster, primaryTarget);
            var count = 0;

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate == null
                    || candidate.IsDead
                    || candidate.Side == caster.Side
                    || candidate.Definition == null
                    || candidate.Definition.heroClass != heroClassFilter)
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

        private static string FormatStatusThemeLabel(string statusThemeKey)
        {
            return string.IsNullOrWhiteSpace(statusThemeKey) ? "any" : statusThemeKey;
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

            ApplyTemporarySkillOverride(caster, skill);

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

            TryRegisterReactiveGuard(context, caster, skill, primaryTarget);

            if (allowActionSequenceTrigger)
            {
                BattleCombatActionSequenceSystem.TryStartSequence(caster, skill, primaryTarget);
            }
        }

        private static void ApplyTemporarySkillOverride(RuntimeHero caster, SkillData skill)
        {
            if (caster == null
                || skill == null
                || skill.activationMode != SkillActivationMode.Active)
            {
                return;
            }

            caster.ApplyTemporarySkillOverride(skill);
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

            var dashDurationSeconds = Mathf.Max(
                0f,
                dashEffect.durationSeconds > Mathf.Epsilon
                    ? dashEffect.durationSeconds
                    : dashEffect.forcedMovementDurationSeconds);

            return new SkillEffectResolutionState(
                dashStartPosition,
                GetDashDestination(dashStartPosition, primaryTarget.CurrentPosition, dashEffect),
                dashDurationSeconds,
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
                case SkillEffectTargetMode.OtherAlliesInRadiusAroundCaster:
                    return CollectUnitsInRadius(context, caster, caster.CurrentPosition, GetEffectRadius(skill, effect), includeAllies: true, excludeCaster: true);
                case SkillEffectTargetMode.EnemiesInRadiusAroundPrimaryTarget:
                    return primaryTarget != null
                        ? CollectUnitsInRadius(context, caster, primaryTarget.CurrentPosition, GetEffectRadius(skill, effect), includeAllies: false)
                        : new List<RuntimeHero>();
                case SkillEffectTargetMode.AlliesInRadiusAroundPrimaryTarget:
                    return primaryTarget != null
                        ? CollectUnitsInRadius(context, caster, primaryTarget.CurrentPosition, GetEffectRadius(skill, effect), includeAllies: true)
                        : new List<RuntimeHero>();
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
                case SkillEffectType.CreateDeployableProxy:
                    CreateDeployableProxies(context, caster, skill, effect, effectTargets, battleManager);
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

                BattleStatsSystem.RecordHealingContribution(context, caster, target, actualHeal);
                context.EventBus.Publish(new HealAppliedEvent(caster, target, actualHeal, skill, target.CurrentHealth));
            }
        }

        private static void ApplyStatusEffectsToTargets(BattleContext context, RuntimeHero caster, SkillData sourceSkill, SkillEffectData effect, List<RuntimeHero> targets)
        {
            for (var i = 0; i < targets.Count; i++)
            {
                ApplyStatuses(context, caster, sourceSkill, effect?.statusEffects, targets[i]);
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
                BattleStatsSystem.RecordForcedMovementContribution(context, caster, target);
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

        private static void ApplyStatuses(
            BattleContext context,
            RuntimeHero caster,
            SkillData sourceSkill,
            IReadOnlyList<StatusEffectData> statuses,
            RuntimeHero target)
        {
            if (statuses == null || target == null || target.IsDead)
            {
                return;
            }

            for (var i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status == null)
                {
                    continue;
                }

                var previousShield = status.effectType == StatusEffectType.Shield
                    ? StatusEffectSystem.GetTotalShield(target)
                    : 0f;
                if (!target.ApplyStatusEffect(status, caster, sourceSkill, caster, out var appliedStatus))
                {
                    continue;
                }

                var appliedSource = appliedStatus?.Source ?? caster;
                BattleStatsSystem.RecordStatusContribution(context, appliedSource, target, status);
                if (status.effectType == StatusEffectType.Shield)
                {
                    var shieldDelta = Mathf.Max(0f, StatusEffectSystem.GetTotalShield(target) - previousShield);
                    BattleStatsSystem.RecordShieldContribution(context, appliedSource, target, shieldDelta);
                }

                context.EventBus.Publish(new StatusAppliedEvent(
                    appliedSource,
                    target,
                    status.effectType,
                    status.durationSeconds,
                    appliedStatus?.Magnitude ?? status.magnitude,
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

        private static void CreateDeployableProxies(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            SkillEffectData effect,
            List<RuntimeHero> effectTargets,
            BattleManager battleManager)
        {
            BattleDeployableProxySystem.CreateDeployableProxies(
                context,
                caster,
                skill,
                effect,
                effectTargets,
                battleManager);
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
            HashSet<string> followUpTriggeredUnitIds = null;
            List<KeyValuePair<RuntimeHero, Vector3>> queuedDeathFollowUps = null;
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || target.IsDead)
                {
                    continue;
                }

                var statusStackCount = GetQueriedStatusStackCount(effect, target);
                if (!MeetsStatusStackRequirement(effect, statusStackCount))
                {
                    continue;
                }

                var damageMultiplier = GetResolvedDamagePowerMultiplier(effect, statusStackCount);
                var targetPosition = target.CurrentPosition;
                var damage = DamageResolver.ResolveDamage(
                    caster.AttackPower,
                    caster.CriticalChance,
                    caster.CriticalDamageMultiplier,
                    target.Defense,
                    context.RandomService,
                    damageMultiplier);
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
                    ApplyStatuses(context, caster, skill, effect?.statusEffects, target);
                    continue;
                }

                if (!target.IsDead && target.CurrentHealth > 0f)
                {
                    ApplyStatuses(context, caster, skill, effect?.statusEffects, target);
                    continue;
                }

                if (effect == null || !effect.triggerFollowUpAreaOnTargetDeath)
                {
                    continue;
                }

                queuedDeathFollowUps ??= new List<KeyValuePair<RuntimeHero, Vector3>>();
                queuedDeathFollowUps.Add(new KeyValuePair<RuntimeHero, Vector3>(target, targetPosition));
            }

            if (queuedDeathFollowUps == null)
            {
                return;
            }

            // All initial targets should finish their base hit resolution before any kill-triggered follow-up area can interfere.
            followUpTriggeredUnitIds ??= new HashSet<string>();
            for (var i = 0; i < queuedDeathFollowUps.Count; i++)
            {
                var queuedFollowUp = queuedDeathFollowUps[i];
                TryTriggerDeathFollowUpArea(
                    context,
                    caster,
                    skill,
                    effect,
                    queuedFollowUp.Key,
                    queuedFollowUp.Value,
                    battleManager,
                    followUpTriggeredUnitIds);
            }
        }

        private static void TryTriggerDeathFollowUpArea(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            SkillEffectData effect,
            RuntimeHero deadTarget,
            Vector3 origin,
            BattleManager battleManager,
            HashSet<string> followUpTriggeredUnitIds)
        {
            if (context == null
                || caster == null
                || skill == null
                || effect == null
                || deadTarget == null
                || !deadTarget.IsDead
                || !effect.triggerFollowUpAreaOnTargetDeath)
            {
                return;
            }

            if (effect.followUpAreaLimitTriggerOncePerUnitPerExecution
                && (followUpTriggeredUnitIds == null || !followUpTriggeredUnitIds.Add(deadTarget.RuntimeId)))
            {
                return;
            }

            var followUpTargets = CollectFollowUpAreaTargets(context, caster, origin, effect);
            for (var i = 0; i < followUpTargets.Count; i++)
            {
                var followUpTarget = followUpTargets[i];
                if (followUpTarget == null || followUpTarget.IsDead)
                {
                    continue;
                }

                var followUpTargetPosition = followUpTarget.CurrentPosition;
                if (effect.followUpAreaPowerMultiplier > Mathf.Epsilon)
                {
                    var followUpDamage = DamageResolver.ResolveDamage(
                        caster.AttackPower,
                        caster.CriticalChance,
                        caster.CriticalDamageMultiplier,
                        followUpTarget.Defense,
                        context.RandomService,
                        effect.followUpAreaPowerMultiplier);
                    BattleDamageSystem.ApplyResolvedDamage(
                        context,
                        battleManager,
                        caster,
                        followUpTarget,
                        followUpDamage,
                        DamageSourceKind.Skill,
                        skill);
                }

                if (!followUpTarget.IsDead)
                {
                    ApplyStatuses(context, caster, skill, effect.followUpAreaStatusEffects, followUpTarget);
                    continue;
                }

                if (!effect.followUpAreaCanChain)
                {
                    continue;
                }

                TryTriggerDeathFollowUpArea(
                    context,
                    caster,
                    skill,
                    effect,
                    followUpTarget,
                    followUpTargetPosition,
                    battleManager,
                    followUpTriggeredUnitIds);
            }
        }

        private static List<RuntimeHero> CollectFollowUpAreaTargets(
            BattleContext context,
            RuntimeHero caster,
            Vector3 origin,
            SkillEffectData effect)
        {
            var results = new List<RuntimeHero>();
            if (context?.Heroes == null || caster == null || effect == null)
            {
                return results;
            }

            var radius = Mathf.Max(0f, effect.followUpAreaRadius);
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var candidate = context.Heroes[i];
                if (candidate == null || candidate.IsDead || candidate.Side == caster.Side)
                {
                    continue;
                }

                if (Vector3.Distance(candidate.CurrentPosition, origin) <= radius)
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        private static int GetQueriedStatusStackCount(SkillEffectData effect, RuntimeHero target)
        {
            if (effect == null
                || target == null
                || effect.statusStackQueryEffectType == StatusEffectType.None)
            {
                return 0;
            }

            return StatusEffectSystem.GetStatusStackCount(
                target,
                effect.statusStackQueryEffectType,
                effect.statusStackQueryThemeKey);
        }

        private static bool MeetsStatusStackRequirement(SkillEffectData effect, int statusStackCount)
        {
            if (effect == null || effect.statusStackQueryEffectType == StatusEffectType.None)
            {
                return true;
            }

            return statusStackCount >= Mathf.Max(0, effect.minimumRequiredStatusStacks);
        }

        private static float GetResolvedDamagePowerMultiplier(SkillEffectData effect, int statusStackCount)
        {
            if (effect == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, effect.powerMultiplier + effect.bonusPowerMultiplierPerStatusStack * Mathf.Max(0, statusStackCount));
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
            bool includeAllies,
            bool excludeCaster = false)
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

                if (excludeCaster && candidate == caster)
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

        private static RuntimeHero SelectThreatenedRangedAllyOrEnemyAnchor(
            BattleContext context,
            RuntimeHero caster,
            SkillData skill,
            float effectiveCastRange)
        {
            if (context == null || caster == null || skill == null)
            {
                return null;
            }

            var threatenedAlly = BattleAiDirector.SelectThreatenedRangedAllyTarget(
                context.Heroes,
                caster,
                effectiveCastRange,
                GetPrioritySearchRadius(skill),
                GetPriorityRequiredUnitCount(skill));
            if (threatenedAlly != null)
            {
                return threatenedAlly;
            }

            return FindDensestEnemyAnchor(context.Heroes, caster, effectiveCastRange, skill.areaRadius);
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
                || skill.targetType == SkillTargetType.LowestHealthRangedAlly
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

        private static float GetPrioritySearchRadius(SkillData skill)
        {
            return skill != null ? Mathf.Max(0f, skill.targetPrioritySearchRadius) : 0f;
        }

        private static int GetPriorityRequiredUnitCount(SkillData skill)
        {
            return skill != null ? Mathf.Max(1, skill.targetPriorityRequiredUnitCount) : 1;
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

        private static bool IsRangedAlly(RuntimeHero candidate, RuntimeHero caster)
        {
            if (candidate == null || caster == null || candidate.Side != caster.Side || candidate.Definition == null)
            {
                return false;
            }

            var tags = candidate.Definition.tags;
            if (tags != null && tags.Contains(HeroTag.Ranged))
            {
                return true;
            }

            return candidate.Definition.basicAttack != null && candidate.Definition.basicAttack.usesProjectile;
        }

        private static void TryRegisterReactiveGuard(BattleContext context, RuntimeHero caster, SkillData skill, RuntimeHero primaryTarget)
        {
            if (context == null
                || caster == null
                || skill?.reactiveGuard == null
                || !skill.reactiveGuard.enabled
                || primaryTarget == null
                || primaryTarget.IsDead
                || primaryTarget.Side != caster.Side)
            {
                return;
            }

            BattleReactiveGuardSystem.RegisterReactiveGuard(context, caster, primaryTarget, skill, skill.reactiveGuard);
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
                : GetDashDestination(startPosition, target.CurrentPosition, effect);
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

        private static Vector3 GetDashDestination(Vector3 casterPosition, Vector3 targetPosition, SkillEffectData effect)
        {
            var offset = targetPosition - casterPosition;
            offset.y = 0f;
            if (offset.sqrMagnitude <= Mathf.Epsilon)
            {
                return Stage01ArenaSpec.ClampPosition(targetPosition);
            }

            var dashDistance = effect != null ? Mathf.Max(0f, effect.forcedMovementDistance) : 0f;
            var destination = dashDistance > Mathf.Epsilon
                ? casterPosition + offset.normalized * dashDistance
                : targetPosition - offset.normalized * 0.8f;
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
