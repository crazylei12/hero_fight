using System;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public interface IBattleEvent
    {
    }

    public enum DamageSourceKind
    {
        Unknown = 0,
        BasicAttack = 1,
        Skill = 2,
        SkillAreaPulse = 3,
        StatusEffect = 4,
        DamageShare = 5,
        CounterTrigger = 6,
        ChanneledPathSkillTick = 7,
    }

    public enum ChanneledPathEndReason
    {
        Completed = 0,
        CasterMissing = 1,
        CasterDead = 2,
        HardControl = 3,
        ForcedMovement = 4,
    }

    public sealed class BattleStartedEvent : IBattleEvent
    {
        public BattleStartedEvent(BattleInputConfig input)
        {
            Input = input;
        }

        public BattleInputConfig Input { get; }
    }

    public sealed class ScoreChangedEvent : IBattleEvent
    {
        public ScoreChangedEvent(int blueKills, int redKills)
        {
            BlueKills = blueKills;
            RedKills = redKills;
        }

        public int BlueKills { get; }

        public int RedKills { get; }
    }

    public sealed class UnitSpawnedEvent : IBattleEvent
    {
        public UnitSpawnedEvent(RuntimeHero hero)
        {
            Hero = hero;
        }

        public RuntimeHero Hero { get; }
    }

    public sealed class AthleteModifierResolvedEvent : IBattleEvent
    {
        public AthleteModifierResolvedEvent(RuntimeHero hero, ResolvedAthleteCombatModifier modifier)
        {
            Hero = hero;
            Modifier = modifier;
        }

        public RuntimeHero Hero { get; }

        public ResolvedAthleteCombatModifier Modifier { get; }
    }

    public sealed class TargetChangedEvent : IBattleEvent
    {
        public TargetChangedEvent(RuntimeHero hero, RuntimeHero target)
        {
            Hero = hero;
            Target = target;
        }

        public RuntimeHero Hero { get; }

        public RuntimeHero Target { get; }
    }

    public sealed class AttackPerformedEvent : IBattleEvent
    {
        public AttackPerformedEvent(RuntimeHero attacker, RuntimeHero target, string variantKey = null)
        {
            Attacker = attacker;
            Target = target;
            VariantKey = variantKey ?? string.Empty;
        }

        public RuntimeHero Attacker { get; }

        public RuntimeHero Target { get; }

        public string VariantKey { get; }
    }

    public sealed class BasicAttackProjectileLaunchedEvent : IBattleEvent
    {
        public BasicAttackProjectileLaunchedEvent(RuntimeBasicAttackProjectile projectile)
        {
            Projectile = projectile;
        }

        public RuntimeBasicAttackProjectile Projectile { get; }
    }

    public sealed class BasicAttackBounceChainResolvedEvent : IBattleEvent
    {
        public BasicAttackBounceChainResolvedEvent(
            RuntimeHero attacker,
            string chainId,
            int bounceHitCount,
            int totalHitCount,
            RuntimeHero firstTarget,
            RuntimeHero lastTarget)
        {
            Attacker = attacker;
            ChainId = chainId ?? string.Empty;
            BounceHitCount = bounceHitCount;
            TotalHitCount = totalHitCount;
            FirstTarget = firstTarget;
            LastTarget = lastTarget;
        }

        public RuntimeHero Attacker { get; }

        public string ChainId { get; }

        public int BounceHitCount { get; }

        public int TotalHitCount { get; }

        public RuntimeHero FirstTarget { get; }

        public RuntimeHero LastTarget { get; }
    }

    public sealed class SkillCastEvent : IBattleEvent
    {
        public SkillCastEvent(RuntimeHero caster, SkillData skill, RuntimeHero primaryTarget, int affectedTargetCount, string variantKey = null)
        {
            Caster = caster;
            Skill = skill;
            PrimaryTarget = primaryTarget;
            AffectedTargetCount = affectedTargetCount;
            VariantKey = variantKey ?? string.Empty;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public RuntimeHero PrimaryTarget { get; }

        public int AffectedTargetCount { get; }

        public string VariantKey { get; }
    }

    public enum UltimateDecisionOutcome
    {
        InvalidPrimaryTarget = 0,
        MissingPrimaryTarget = 1,
        NoAffectedTargets = 2,
        InsufficientAffectedTargets = 3,
        DecisionConditionsFailed = 4,
        LegacyOpportunityFailed = 5,
        RollFailed = 6,
        RollPassed = 7,
    }

    public sealed class UltimateDecisionEvaluatedEvent : IBattleEvent
    {
        public UltimateDecisionEvaluatedEvent(
            RuntimeHero caster,
            SkillData skill,
            RuntimeHero primaryTarget,
            bool usesTemplateDecision,
            int affectedTargetCount,
            int fallbackStage,
            UltimateDecisionOutcome outcome,
            string decisionSummary,
            bool chanceEvaluated,
            string chanceSummary,
            float finalChance,
            float suppressionMultiplier,
            float timeSinceLastAllyUltimateSeconds,
            bool rollEvaluated,
            float rollValue,
            bool rollPassed,
            float nextDecisionCheckTimeSeconds)
        {
            Caster = caster;
            Skill = skill;
            PrimaryTarget = primaryTarget;
            UsesTemplateDecision = usesTemplateDecision;
            AffectedTargetCount = affectedTargetCount;
            FallbackStage = fallbackStage;
            Outcome = outcome;
            DecisionSummary = decisionSummary ?? string.Empty;
            ChanceEvaluated = chanceEvaluated;
            ChanceSummary = chanceSummary ?? string.Empty;
            FinalChance = finalChance;
            SuppressionMultiplier = suppressionMultiplier;
            TimeSinceLastAllyUltimateSeconds = timeSinceLastAllyUltimateSeconds;
            RollEvaluated = rollEvaluated;
            RollValue = rollValue;
            RollPassed = rollPassed;
            NextDecisionCheckTimeSeconds = nextDecisionCheckTimeSeconds;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public RuntimeHero PrimaryTarget { get; }

        public bool UsesTemplateDecision { get; }

        public int AffectedTargetCount { get; }

        public int FallbackStage { get; }

        public UltimateDecisionOutcome Outcome { get; }

        public string DecisionSummary { get; }

        public bool ChanceEvaluated { get; }

        public string ChanceSummary { get; }

        public float FinalChance { get; }

        public float SuppressionMultiplier { get; }

        public float TimeSinceLastAllyUltimateSeconds { get; }

        public bool RollEvaluated { get; }

        public float RollValue { get; }

        public bool RollPassed { get; }

        public float NextDecisionCheckTimeSeconds { get; }
    }

    public sealed class SkillAreaCreatedEvent : IBattleEvent
    {
        public SkillAreaCreatedEvent(RuntimeHero caster, SkillData skill, RuntimeSkillArea area)
        {
            Caster = caster;
            Skill = skill;
            Area = area;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public RuntimeSkillArea Area { get; }
    }

    public sealed class SkillAreaPulseEvent : IBattleEvent
    {
        public SkillAreaPulseEvent(RuntimeHero caster, SkillData skill, RuntimeSkillArea area, int affectedTargetCount)
        {
            Caster = caster;
            Skill = skill;
            Area = area;
            AffectedTargetCount = affectedTargetCount;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public RuntimeSkillArea Area { get; }

        public int AffectedTargetCount { get; }
    }

    public sealed class RadialSweepResolvedEvent : IBattleEvent
    {
        public RadialSweepResolvedEvent(
            RuntimeHero caster,
            SkillData skill,
            string sweepId,
            RadialSweepDirectionMode direction,
            Vector3 center,
            float maxRadius,
            int hitCount)
        {
            Caster = caster;
            Skill = skill;
            SweepId = sweepId ?? string.Empty;
            Direction = direction;
            Center = center;
            MaxRadius = maxRadius;
            HitCount = hitCount;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public string SweepId { get; }

        public RadialSweepDirectionMode Direction { get; }

        public Vector3 Center { get; }

        public float MaxRadius { get; }

        public int HitCount { get; }
    }

    public sealed class ReturningPathStrikeQueuedEvent : IBattleEvent
    {
        public ReturningPathStrikeQueuedEvent(
            RuntimeHero caster,
            SkillData skill,
            string strikeId,
            ReturningPathStrikePhase phase,
            Vector3 startPosition,
            Vector3 endPosition,
            float pathWidth,
            float delaySeconds,
            float travelDurationSeconds)
        {
            Caster = caster;
            Skill = skill;
            StrikeId = strikeId ?? string.Empty;
            Phase = phase;
            StartPosition = startPosition;
            EndPosition = endPosition;
            PathWidth = pathWidth;
            DelaySeconds = delaySeconds;
            TravelDurationSeconds = travelDurationSeconds;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public string StrikeId { get; }

        public ReturningPathStrikePhase Phase { get; }

        public Vector3 StartPosition { get; }

        public Vector3 EndPosition { get; }

        public float PathWidth { get; }

        public float DelaySeconds { get; }

        public float TravelDurationSeconds { get; }
    }

    public sealed class ReturningPathStrikeResolvedEvent : IBattleEvent
    {
        public ReturningPathStrikeResolvedEvent(
            RuntimeHero caster,
            SkillData skill,
            string strikeId,
            ReturningPathStrikePhase phase,
            Vector3 startPosition,
            Vector3 endPosition,
            float pathWidth,
            int hitCount)
        {
            Caster = caster;
            Skill = skill;
            StrikeId = strikeId ?? string.Empty;
            Phase = phase;
            StartPosition = startPosition;
            EndPosition = endPosition;
            PathWidth = pathWidth;
            HitCount = hitCount;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public string StrikeId { get; }

        public ReturningPathStrikePhase Phase { get; }

        public Vector3 StartPosition { get; }

        public Vector3 EndPosition { get; }

        public float PathWidth { get; }

        public int HitCount { get; }
    }

    public sealed class ChanneledPathSkillStartedEvent : IBattleEvent
    {
        public ChanneledPathSkillStartedEvent(
            RuntimeHero caster,
            SkillData skill,
            string channelId,
            Vector3 startPosition,
            Vector3 endPosition,
            float pathWidth,
            float chargeDurationSeconds,
            float channelDurationSeconds,
            int expectedTickCount)
        {
            Caster = caster;
            Skill = skill;
            ChannelId = channelId ?? string.Empty;
            StartPosition = startPosition;
            EndPosition = endPosition;
            PathWidth = pathWidth;
            ChargeDurationSeconds = chargeDurationSeconds;
            ChannelDurationSeconds = channelDurationSeconds;
            ExpectedTickCount = expectedTickCount;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public string ChannelId { get; }

        public Vector3 StartPosition { get; }

        public Vector3 EndPosition { get; }

        public float PathWidth { get; }

        public float ChargeDurationSeconds { get; }

        public float ChannelDurationSeconds { get; }

        public int ExpectedTickCount { get; }
    }

    public sealed class ChanneledPathSkillTickEvent : IBattleEvent
    {
        public ChanneledPathSkillTickEvent(
            RuntimeHero caster,
            SkillData skill,
            string channelId,
            int tickIndex,
            Vector3 startPosition,
            Vector3 endPosition,
            float pathWidth,
            int hitCount)
        {
            Caster = caster;
            Skill = skill;
            ChannelId = channelId ?? string.Empty;
            TickIndex = tickIndex;
            StartPosition = startPosition;
            EndPosition = endPosition;
            PathWidth = pathWidth;
            HitCount = hitCount;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public string ChannelId { get; }

        public int TickIndex { get; }

        public Vector3 StartPosition { get; }

        public Vector3 EndPosition { get; }

        public float PathWidth { get; }

        public int HitCount { get; }
    }

    public sealed class ChanneledPathSkillEndedEvent : IBattleEvent
    {
        public ChanneledPathSkillEndedEvent(
            RuntimeHero caster,
            SkillData skill,
            string channelId,
            ChanneledPathEndReason reason,
            int resolvedTickCount)
        {
            Caster = caster;
            Skill = skill;
            ChannelId = channelId ?? string.Empty;
            Reason = reason;
            ResolvedTickCount = resolvedTickCount;
        }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public string ChannelId { get; }

        public ChanneledPathEndReason Reason { get; }

        public int ResolvedTickCount { get; }
    }

    public sealed class DamageAppliedEvent : IBattleEvent
    {
        public DamageAppliedEvent(
            RuntimeHero attacker,
            RuntimeHero target,
            float damageAmount,
            DamageSourceKind sourceKind = DamageSourceKind.Unknown,
            SkillData sourceSkill = null,
            float remainingHealth = 0f,
            string sourceBasicAttackVariantKey = null,
            RuntimeDeployableProxy sourceProxy = null)
        {
            Attacker = attacker;
            Target = target;
            DamageAmount = damageAmount;
            SourceKind = sourceKind;
            SourceSkill = sourceSkill;
            RemainingHealth = remainingHealth;
            SourceBasicAttackVariantKey = sourceBasicAttackVariantKey ?? string.Empty;
            SourceProxy = sourceProxy;
        }

        public RuntimeHero Attacker { get; }

        public RuntimeHero Target { get; }

        public float DamageAmount { get; }

        public DamageSourceKind SourceKind { get; }

        public SkillData SourceSkill { get; }

        public float RemainingHealth { get; }

        public string SourceBasicAttackVariantKey { get; }

        public RuntimeDeployableProxy SourceProxy { get; }
    }

    public sealed class HealAppliedEvent : IBattleEvent
    {
        public HealAppliedEvent(
            RuntimeHero caster,
            RuntimeHero target,
            float healAmount,
            SkillData sourceSkill = null,
            float resultingHealth = 0f,
            string sourceBasicAttackVariantKey = null,
            RuntimeDeployableProxy sourceProxy = null)
        {
            Caster = caster;
            Target = target;
            HealAmount = healAmount;
            SourceSkill = sourceSkill;
            ResultingHealth = resultingHealth;
            SourceBasicAttackVariantKey = sourceBasicAttackVariantKey ?? string.Empty;
            SourceProxy = sourceProxy;
        }

        public RuntimeHero Caster { get; }

        public RuntimeHero Target { get; }

        public float HealAmount { get; }

        public SkillData SourceSkill { get; }

        public float ResultingHealth { get; }

        public string SourceBasicAttackVariantKey { get; }

        public RuntimeDeployableProxy SourceProxy { get; }
    }

    public sealed class SelfHealthCostAppliedEvent : IBattleEvent
    {
        public SelfHealthCostAppliedEvent(
            RuntimeHero hero,
            float healthCostAmount,
            SkillData sourceSkill = null,
            float resultingHealth = 0f,
            string sourceBasicAttackVariantKey = null)
        {
            Hero = hero;
            HealthCostAmount = Mathf.Max(0f, healthCostAmount);
            SourceSkill = sourceSkill;
            ResultingHealth = resultingHealth;
            SourceBasicAttackVariantKey = sourceBasicAttackVariantKey ?? string.Empty;
        }

        public RuntimeHero Hero { get; }

        public float HealthCostAmount { get; }

        public SkillData SourceSkill { get; }

        public float ResultingHealth { get; }

        public string SourceBasicAttackVariantKey { get; }
    }

    public sealed class StatusAppliedEvent : IBattleEvent
    {
        public StatusAppliedEvent(
            RuntimeHero source,
            RuntimeHero target,
            StatusEffectType effectType,
            float durationSeconds,
            float magnitude,
            SkillData sourceSkill = null,
            RuntimeHero appliedBy = null)
        {
            Source = source;
            Target = target;
            EffectType = effectType;
            DurationSeconds = durationSeconds;
            Magnitude = magnitude;
            SourceSkill = sourceSkill;
            AppliedBy = appliedBy ?? source;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero AppliedBy { get; }

        public RuntimeHero Target { get; }

        public StatusEffectType EffectType { get; }

        public float DurationSeconds { get; }

        public float Magnitude { get; }

        public SkillData SourceSkill { get; }
    }

    public sealed class KnockUpFollowUpTriggeredEvent : IBattleEvent
    {
        public KnockUpFollowUpTriggeredEvent(
            RuntimeHero follower,
            SkillData followUpSkill,
            RuntimeHero triggerSource,
            SkillData triggerSkill,
            string triggerKind,
            int affectedTargetCount,
            RuntimeHero landingAnchor,
            Vector3 landingDestination,
            bool usedFallbackLanding,
            float damagePowerMultiplier)
        {
            Follower = follower;
            FollowUpSkill = followUpSkill;
            TriggerSource = triggerSource;
            TriggerSkill = triggerSkill;
            TriggerKind = string.IsNullOrWhiteSpace(triggerKind) ? "KnockUp" : triggerKind;
            AffectedTargetCount = Mathf.Max(0, affectedTargetCount);
            LandingAnchor = landingAnchor;
            LandingDestination = landingDestination;
            UsedFallbackLanding = usedFallbackLanding;
            DamagePowerMultiplier = Mathf.Max(0f, damagePowerMultiplier);
        }

        public RuntimeHero Follower { get; }

        public SkillData FollowUpSkill { get; }

        public RuntimeHero TriggerSource { get; }

        public SkillData TriggerSkill { get; }

        public string TriggerKind { get; }

        public int AffectedTargetCount { get; }

        public RuntimeHero LandingAnchor { get; }

        public Vector3 LandingDestination { get; }

        public bool UsedFallbackLanding { get; }

        public float DamagePowerMultiplier { get; }
    }

    public sealed class StatusRemovedEvent : IBattleEvent
    {
        public StatusRemovedEvent(
            RuntimeHero source,
            RuntimeHero target,
            StatusEffectType effectType,
            SkillData sourceSkill = null,
            RuntimeHero appliedBy = null)
        {
            Source = source;
            Target = target;
            EffectType = effectType;
            SourceSkill = sourceSkill;
            AppliedBy = appliedBy ?? source;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero AppliedBy { get; }

        public RuntimeHero Target { get; }

        public StatusEffectType EffectType { get; }

        public SkillData SourceSkill { get; }
    }

    public enum FocusFireCommandTargetChangeReason
    {
        Started = 0,
        TargetInvalid = 1,
        Expired = 2,
        Replaced = 3,
    }

    public sealed class FocusFireCommandTargetChangedEvent : IBattleEvent
    {
        public FocusFireCommandTargetChangedEvent(
            RuntimeHero source,
            SkillData skill,
            RuntimeHero previousTarget,
            RuntimeHero currentTarget,
            float remainingDurationSeconds,
            FocusFireCommandTargetChangeReason reason)
        {
            Source = source;
            Skill = skill;
            PreviousTarget = previousTarget;
            CurrentTarget = currentTarget;
            RemainingDurationSeconds = Mathf.Max(0f, remainingDurationSeconds);
            Reason = reason;
        }

        public RuntimeHero Source { get; }

        public SkillData Skill { get; }

        public RuntimeHero PreviousTarget { get; }

        public RuntimeHero CurrentTarget { get; }

        public float RemainingDurationSeconds { get; }

        public FocusFireCommandTargetChangeReason Reason { get; }
    }

    public sealed class ForcedMovementAppliedEvent : IBattleEvent
    {
        public ForcedMovementAppliedEvent(
            RuntimeHero source,
            RuntimeHero target,
            Vector3 startPosition,
            Vector3 destination,
            float durationSeconds,
            float peakHeight,
            SkillData sourceSkill = null,
            bool countsAsKnockback = false)
        {
            Source = source;
            Target = target;
            StartPosition = startPosition;
            Destination = destination;
            DurationSeconds = durationSeconds;
            PeakHeight = peakHeight;
            SourceSkill = sourceSkill;
            CountsAsKnockback = countsAsKnockback;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero Target { get; }

        public Vector3 StartPosition { get; }

        public Vector3 Destination { get; }

        public float DurationSeconds { get; }

        public float PeakHeight { get; }

        public SkillData SourceSkill { get; }

        public bool CountsAsKnockback { get; }
    }

    public enum PassiveSkillValueType
    {
        AttackPower = 0,
        Defense = 1,
        Lifesteal = 2,
    }

    public sealed class PassiveSkillValueChangedEvent : IBattleEvent
    {
        public PassiveSkillValueChangedEvent(
            RuntimeHero hero,
            SkillData skill,
            PassiveSkillValueType valueType,
            float modifierMultiplier)
        {
            Hero = hero;
            Skill = skill;
            ValueType = valueType;
            ModifierMultiplier = modifierMultiplier;
        }

        public RuntimeHero Hero { get; }

        public SkillData Skill { get; }

        public PassiveSkillValueType ValueType { get; }

        public float ModifierMultiplier { get; }
    }

    public sealed class PassivePeriodicHealRateChangedEvent : IBattleEvent
    {
        public PassivePeriodicHealRateChangedEvent(
            RuntimeHero hero,
            SkillData skill,
            float currentHealthRatio,
            float healPercentMaxHealthPerTick,
            float tickIntervalSeconds)
        {
            Hero = hero;
            Skill = skill;
            CurrentHealthRatio = currentHealthRatio;
            HealPercentMaxHealthPerTick = healPercentMaxHealthPerTick;
            TickIntervalSeconds = tickIntervalSeconds;
        }

        public RuntimeHero Hero { get; }

        public SkillData Skill { get; }

        public float CurrentHealthRatio { get; }

        public float HealPercentMaxHealthPerTick { get; }

        public float TickIntervalSeconds { get; }
    }

    public sealed class PassiveStackChangedEvent : IBattleEvent
    {
        public PassiveStackChangedEvent(
            RuntimeHero hero,
            SkillData skill,
            int previousStackCount,
            int currentStackCount,
            int maxStacks,
            float attackPowerBonusMultiplier,
            float attackSpeedBonusMultiplier,
            float healAmount)
        {
            Hero = hero;
            Skill = skill;
            PreviousStackCount = previousStackCount;
            CurrentStackCount = currentStackCount;
            MaxStacks = maxStacks;
            AttackPowerBonusMultiplier = attackPowerBonusMultiplier;
            AttackSpeedBonusMultiplier = attackSpeedBonusMultiplier;
            HealAmount = healAmount;
        }

        public RuntimeHero Hero { get; }

        public SkillData Skill { get; }

        public int PreviousStackCount { get; }

        public int CurrentStackCount { get; }

        public int MaxStacks { get; }

        public float AttackPowerBonusMultiplier { get; }

        public float AttackSpeedBonusMultiplier { get; }

        public float HealAmount { get; }
    }

    public sealed class StatusCounterChangedEvent : IBattleEvent
    {
        public StatusCounterChangedEvent(
            RuntimeHero source,
            RuntimeHero target,
            StatusEffectType effectType,
            string statusThemeKey,
            SkillData sourceSkill,
            DamageSourceKind sourceKind,
            int previousStackCount,
            int currentStackCount,
            int maxStacks)
        {
            Source = source;
            Target = target;
            EffectType = effectType;
            StatusThemeKey = statusThemeKey ?? string.Empty;
            SourceSkill = sourceSkill;
            SourceKind = sourceKind;
            PreviousStackCount = previousStackCount;
            CurrentStackCount = currentStackCount;
            MaxStacks = maxStacks;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero Target { get; }

        public StatusEffectType EffectType { get; }

        public string StatusThemeKey { get; }

        public SkillData SourceSkill { get; }

        public DamageSourceKind SourceKind { get; }

        public int PreviousStackCount { get; }

        public int CurrentStackCount { get; }

        public int MaxStacks { get; }
    }

    public sealed class StatusCounterThresholdTriggeredEvent : IBattleEvent
    {
        public StatusCounterThresholdTriggeredEvent(
            RuntimeHero source,
            RuntimeHero target,
            StatusEffectType effectType,
            string statusThemeKey,
            SkillData sourceSkill,
            DamageSourceKind sourceKind,
            int threshold,
            int clearedStackCount)
        {
            Source = source;
            Target = target;
            EffectType = effectType;
            StatusThemeKey = statusThemeKey ?? string.Empty;
            SourceSkill = sourceSkill;
            SourceKind = sourceKind;
            Threshold = threshold;
            ClearedStackCount = clearedStackCount;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero Target { get; }

        public StatusEffectType EffectType { get; }

        public string StatusThemeKey { get; }

        public SkillData SourceSkill { get; }

        public DamageSourceKind SourceKind { get; }

        public int Threshold { get; }

        public int ClearedStackCount { get; }
    }

    public sealed class PositiveEffectRejectedEvent : IBattleEvent
    {
        public PositiveEffectRejectedEvent(
            RuntimeHero source,
            RuntimeHero target,
            string effectLabel,
            SkillData sourceSkill = null,
            string sourceBasicAttackVariantKey = null)
        {
            Source = source;
            Target = target;
            EffectLabel = effectLabel ?? string.Empty;
            SourceSkill = sourceSkill;
            SourceBasicAttackVariantKey = sourceBasicAttackVariantKey ?? string.Empty;
        }

        public RuntimeHero Source { get; }

        public RuntimeHero Target { get; }

        public string EffectLabel { get; }

        public SkillData SourceSkill { get; }

        public string SourceBasicAttackVariantKey { get; }
    }

    public sealed class SkillTemporaryOverrideChangedEvent : IBattleEvent
    {
        public SkillTemporaryOverrideChangedEvent(
            RuntimeHero hero,
            SkillData skill,
            bool isActive,
            float lifestealRatio,
            float visualScaleMultiplier,
            float visualTintStrength)
        {
            Hero = hero;
            Skill = skill;
            IsActive = isActive;
            LifestealRatio = lifestealRatio;
            VisualScaleMultiplier = visualScaleMultiplier;
            VisualTintStrength = visualTintStrength;
        }

        public RuntimeHero Hero { get; }

        public SkillData Skill { get; }

        public bool IsActive { get; }

        public float LifestealRatio { get; }

        public float VisualScaleMultiplier { get; }

        public float VisualTintStrength { get; }
    }

    public sealed class ReactiveGuardTriggeredEvent : IBattleEvent
    {
        public ReactiveGuardTriggeredEvent(RuntimeHero caster, RuntimeHero protectedHero, SkillData sourceSkill, int affectedTargetCount)
        {
            Caster = caster;
            ProtectedHero = protectedHero;
            SourceSkill = sourceSkill;
            AffectedTargetCount = affectedTargetCount;
        }

        public RuntimeHero Caster { get; }

        public RuntimeHero ProtectedHero { get; }

        public SkillData SourceSkill { get; }

        public int AffectedTargetCount { get; }
    }

    public sealed class ReactiveCounterTriggeredEvent : IBattleEvent
    {
        public ReactiveCounterTriggeredEvent(RuntimeHero defender, RuntimeHero attacker, SkillData sourceSkill, float counterDamage)
        {
            Defender = defender;
            Attacker = attacker;
            SourceSkill = sourceSkill;
            CounterDamage = Mathf.Max(0f, counterDamage);
        }

        public RuntimeHero Defender { get; }

        public RuntimeHero Attacker { get; }

        public SkillData SourceSkill { get; }

        public float CounterDamage { get; }
    }

    public sealed class UnitDiedEvent : IBattleEvent
    {
        public UnitDiedEvent(RuntimeHero victim, RuntimeHero killer)
        {
            Victim = victim;
            Killer = killer;
        }

        public RuntimeHero Victim { get; }

        public RuntimeHero Killer { get; }
    }

    public sealed class UnitRevivedEvent : IBattleEvent
    {
        public UnitRevivedEvent(RuntimeHero hero)
        {
            Hero = hero;
        }

        public RuntimeHero Hero { get; }
    }

    public enum CloneUnitRemovalReason
    {
        Expired = 0,
        Killed = 1,
        Replaced = 2,
        OwnerUnavailable = 3,
    }

    public sealed class CloneUnitSpawnedEvent : IBattleEvent
    {
        public CloneUnitSpawnedEvent(RuntimeHero clone, RuntimeHero owner, RuntimeHero source, SkillData sourceSkill)
        {
            Clone = clone;
            Owner = owner;
            Source = source;
            SourceSkill = sourceSkill;
        }

        public RuntimeHero Clone { get; }

        public RuntimeHero Owner { get; }

        public RuntimeHero Source { get; }

        public SkillData SourceSkill { get; }
    }

    public sealed class CloneUnitRemovedEvent : IBattleEvent
    {
        public CloneUnitRemovedEvent(RuntimeHero clone, CloneUnitRemovalReason reason)
        {
            Clone = clone;
            Reason = reason;
        }

        public RuntimeHero Clone { get; }

        public CloneUnitRemovalReason Reason { get; }
    }

    public sealed class OvertimeStartedEvent : IBattleEvent
    {
    }

    public enum DeployableProxyRemovalReason
    {
        Expired = 0,
        Replaced = 1,
        Triggered = 2,
    }

    public sealed class DeployableProxySpawnedEvent : IBattleEvent
    {
        public DeployableProxySpawnedEvent(RuntimeDeployableProxy proxy)
        {
            Proxy = proxy;
        }

        public RuntimeDeployableProxy Proxy { get; }
    }

    public sealed class DeployableProxyRemovedEvent : IBattleEvent
    {
        public DeployableProxyRemovedEvent(RuntimeDeployableProxy proxy, DeployableProxyRemovalReason reason)
        {
            Proxy = proxy;
            Reason = reason;
        }

        public RuntimeDeployableProxy Proxy { get; }

        public DeployableProxyRemovalReason Reason { get; }
    }

    public sealed class DeployableProxyPulseEvent : IBattleEvent
    {
        public DeployableProxyPulseEvent(RuntimeDeployableProxy proxy, int affectedTargetCount)
        {
            Proxy = proxy;
            AffectedTargetCount = Mathf.Max(0, affectedTargetCount);
        }

        public RuntimeDeployableProxy Proxy { get; }

        public int AffectedTargetCount { get; }
    }

    public sealed class BattleEndedEvent : IBattleEvent
    {
        public BattleEndedEvent(BattleResultData result)
        {
            Result = result;
        }

        public BattleResultData Result { get; }
    }

    public class BattleEventBus
    {
        public event Action<IBattleEvent> Published;

        public void Publish(IBattleEvent battleEvent)
        {
            Published?.Invoke(battleEvent);
        }
    }
}
