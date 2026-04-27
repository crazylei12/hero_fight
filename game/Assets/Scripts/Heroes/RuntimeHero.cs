using System;
using System.Collections.Generic;
using Fight.Battle;
using Fight.Data;
using UnityEngine;

namespace Fight.Heroes
{
    public enum CombatActionType
    {
        BasicAttack = 0,
        SkillCast = 1,
    }

    public sealed class PendingCombatAction
    {
        public PendingCombatAction(
            RuntimeHero target,
            ResolvedBasicAttack basicAttack,
            bool suppressActionSequenceTrigger = false,
            bool isActionSequenceStep = false)
        {
            ActionType = CombatActionType.BasicAttack;
            Target = target;
            BasicAttack = basicAttack;
            AffectedTargets = Array.Empty<RuntimeHero>();
            SuppressActionSequenceTrigger = suppressActionSequenceTrigger;
            IsActionSequenceStep = isActionSequenceStep;
        }

        public PendingCombatAction(
            ResolvedSkillCast resolvedSkill,
            RuntimeHero primaryTarget,
            IReadOnlyList<RuntimeHero> affectedTargets,
            bool suppressActionSequenceTrigger = false,
            bool isActionSequenceStep = false)
        {
            ActionType = CombatActionType.SkillCast;
            ResolvedSkill = resolvedSkill;
            Skill = resolvedSkill?.Skill;
            PrimaryTarget = primaryTarget;
            SuppressActionSequenceTrigger = suppressActionSequenceTrigger;
            IsActionSequenceStep = isActionSequenceStep;
            if (affectedTargets != null)
            {
                AffectedTargets = new List<RuntimeHero>(affectedTargets);
            }
            else
            {
                AffectedTargets = Array.Empty<RuntimeHero>();
            }
        }

        public CombatActionType ActionType { get; }

        public RuntimeHero Target { get; }

        public ResolvedBasicAttack BasicAttack { get; }

        public SkillData Skill { get; }

        public ResolvedSkillCast ResolvedSkill { get; }

        public RuntimeHero PrimaryTarget { get; }

        public IReadOnlyList<RuntimeHero> AffectedTargets { get; }

        public bool SuppressActionSequenceTrigger { get; }

        public bool IsActionSequenceStep { get; }
    }

    public class RuntimeHero
    {
        private const float DefaultKnockUpVisualPeakHeight = 0.72f;

        private sealed class RuntimeSkillTemporaryOverride
        {
            public RuntimeSkillTemporaryOverride(SkillData sourceSkill, SkillTemporaryOverrideData definition)
            {
                SourceSkill = sourceSkill;
                Refresh(definition);
            }

            public SkillData SourceSkill { get; }

            public float RemainingDurationSeconds { get; private set; }

            public SkillTemporaryOverrideLifestealMode LifestealMode { get; private set; }

            public float LifestealRatio { get; private set; }

            public float VisualScaleMultiplier { get; private set; }

            public Color VisualTintColor { get; private set; }

            public float VisualTintStrength { get; private set; }

            public void Refresh(SkillTemporaryOverrideData definition)
            {
                RemainingDurationSeconds = definition != null ? Mathf.Max(0f, definition.durationSeconds) : 0f;
                LifestealMode = definition != null ? definition.lifestealMode : SkillTemporaryOverrideLifestealMode.Additive;
                LifestealRatio = definition != null ? Mathf.Max(0f, definition.lifestealRatio) : 0f;
                VisualScaleMultiplier = definition != null ? Mathf.Max(1f, definition.visualScaleMultiplier) : 1f;
                VisualTintColor = definition != null ? definition.visualTintColor : Color.white;
                VisualTintStrength = definition != null ? Mathf.Clamp01(definition.visualTintStrength) : 0f;
            }

            public void Tick(float deltaTime)
            {
                RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - Mathf.Max(0f, deltaTime));
            }

            public bool IsExpired => RemainingDurationSeconds <= Mathf.Epsilon;
        }

        private sealed class RuntimeCombatFormOverride
        {
            public RuntimeCombatFormOverride(SkillData sourceSkill, CombatFormOverrideData definition)
            {
                SourceSkill = sourceSkill;
                Refresh(definition);
            }

            public SkillData SourceSkill { get; }

            public string FormKey { get; private set; }

            public float RemainingDurationSeconds { get; private set; }

            public bool HasFiniteDuration { get; private set; }

            public bool ExpiresOnDeath { get; private set; }

            public bool OverrideUsesProjectile { get; private set; }

            public bool UsesProjectile { get; private set; }

            public float AttackRangeOverride { get; private set; }

            public float ProjectileSpeedOverride { get; private set; }

            public float AttackPowerModifier { get; private set; }

            public float AttackSpeedModifier { get; private set; }

            public void Refresh(CombatFormOverrideData definition)
            {
                FormKey = definition != null ? definition.formKey ?? string.Empty : string.Empty;
                HasFiniteDuration = definition != null && definition.durationSeconds > Mathf.Epsilon;
                RemainingDurationSeconds = HasFiniteDuration ? Mathf.Max(0f, definition.durationSeconds) : 0f;
                ExpiresOnDeath = definition == null || definition.expiresOnDeath;
                OverrideUsesProjectile = definition != null && definition.overrideUsesProjectile;
                UsesProjectile = definition != null && definition.usesProjectile;
                AttackRangeOverride = definition != null ? Mathf.Max(0f, definition.attackRangeOverride) : 0f;
                ProjectileSpeedOverride = definition != null ? Mathf.Max(0f, definition.projectileSpeedOverride) : 0f;
                AttackPowerModifier = definition != null ? definition.attackPowerModifier : 0f;
                AttackSpeedModifier = definition != null ? definition.attackSpeedModifier : 0f;
            }

            public void Tick(float deltaTime)
            {
                if (!HasFiniteDuration)
                {
                    return;
                }

                RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - Mathf.Max(0f, deltaTime));
            }

            public bool IsExpired => HasFiniteDuration && RemainingDurationSeconds <= Mathf.Epsilon;
        }

        private sealed class RuntimeReactiveCounterStance
        {
            private readonly Dictionary<string, float> lastTriggerTimeBySourceId = new Dictionary<string, float>();

            public RuntimeReactiveCounterStance(SkillData sourceSkill, ReactiveCounterData definition)
            {
                SourceSkill = sourceSkill;
                Refresh(definition);
            }

            public SkillData SourceSkill { get; }

            public ReactiveCounterData Definition { get; private set; }

            public float RemainingDurationSeconds { get; private set; }

            public bool BlocksBasicAttacks => Definition != null && Definition.blocksBasicAttacks;

            public bool BlocksSkillCasts => Definition != null && Definition.blocksSkillCasts;

            public void Refresh(ReactiveCounterData definition)
            {
                Definition = definition;
                RemainingDurationSeconds = definition != null ? Mathf.Max(0f, definition.durationSeconds) : 0f;
                lastTriggerTimeBySourceId.Clear();
            }

            public void Tick(float deltaTime)
            {
                RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - Mathf.Max(0f, deltaTime));
            }

            public bool TryConsumeTrigger(RuntimeHero source, float currentTimeSeconds)
            {
                if (source == null || Definition == null)
                {
                    return false;
                }

                var sourceKey = !string.IsNullOrWhiteSpace(source.RuntimeId)
                    ? source.RuntimeId
                    : source.GetHashCode().ToString();
                var cooldownSeconds = Mathf.Max(0f, Definition.sourceTriggerCooldownSeconds);
                if (cooldownSeconds > Mathf.Epsilon
                    && lastTriggerTimeBySourceId.TryGetValue(sourceKey, out var lastTriggerTime)
                    && currentTimeSeconds - lastTriggerTime < cooldownSeconds)
                {
                    return false;
                }

                lastTriggerTimeBySourceId[sourceKey] = Mathf.Max(0f, currentTimeSeconds);
                return true;
            }

            public bool IsExpired => RemainingDurationSeconds <= Mathf.Epsilon;
        }

        private sealed class RuntimeContributionRecord
        {
            public RuntimeContributionRecord(RuntimeHero contributor, float timeSeconds)
            {
                Contributor = contributor;
                LastContributionTimeSeconds = Mathf.Max(0f, timeSeconds);
            }

            public RuntimeHero Contributor { get; }

            public float LastContributionTimeSeconds { get; private set; }

            public void Refresh(float timeSeconds)
            {
                LastContributionTimeSeconds = Mathf.Max(0f, timeSeconds);
            }
        }

        private sealed class RuntimePassiveSkillState
        {
            public RuntimePassiveSkillState(SkillData sourceSkill)
            {
                SourceSkill = sourceSkill;
            }

            public SkillData SourceSkill { get; }

            public int KillParticipationStacks { get; set; }

            public bool HasInitializedPeriodicSelfHealTimer { get; private set; }

            public float PeriodicSelfHealTickRemainingSeconds { get; set; }

            public float LastReportedPeriodicSelfHealPercentMaxHealth { get; set; } = -1f;

            public void InitializePeriodicSelfHealTimer(float intervalSeconds)
            {
                HasInitializedPeriodicSelfHealTimer = true;
                PeriodicSelfHealTickRemainingSeconds = Mathf.Max(0.1f, intervalSeconds);
            }

            public void ResetForSpawn()
            {
                HasInitializedPeriodicSelfHealTimer = false;
                PeriodicSelfHealTickRemainingSeconds = 0f;
                LastReportedPeriodicSelfHealPercentMaxHealth = -1f;
            }
        }

        public RuntimeHero(HeroDefinition definition, TeamSide side, Vector3 spawnPosition, int slotIndex)
        {
            Definition = definition;
            Side = side;
            SpawnPosition = spawnPosition;
            SlotIndex = slotIndex;
            CurrentPosition = spawnPosition;
            ResetToSpawn();
        }

        public HeroDefinition Definition { get; }

        public TeamSide Side { get; }

        public Vector3 SpawnPosition { get; }

        public int SlotIndex { get; }

        public string RuntimeId => $"{Side}_{SlotIndex}_{Definition?.heroId}";

        public Vector3 CurrentPosition { get; set; }

        public float CurrentHealth { get; private set; }

        public RuntimeHero CurrentTarget { get; private set; }

        public float RespawnRemainingSeconds { get; private set; }

        public float AttackCooldownRemainingSeconds { get; private set; }

        public int CurrentBasicAttackVariantIndex { get; private set; }

        public int Kills { get; private set; }

        public int Deaths { get; private set; }

        public float DamageDealt { get; private set; }

        public float DamageTaken { get; private set; }

        public float HealingDone { get; private set; }

        public float ShieldingDone { get; private set; }

        public float HealingAndShieldingDone => HealingDone + ShieldingDone;

        public int Assists { get; private set; }

        public int ActiveSkillCastCount { get; private set; }

        public int UltimateCastCount { get; private set; }

        public bool IsDead { get; private set; }

        public float CombatEngagedSeconds { get; private set; }

        public float CurrentBattleTimeSeconds { get; private set; }

        public IReadOnlyList<RuntimeStatusEffect> ActiveStatusEffects => activeStatusEffects;

        internal List<RuntimeStatusEffect> MutableStatusEffects => activeStatusEffects;

        public float VisualHeightOffset { get; private set; }

        public bool HasHardControl => StatusEffectSystem.HasHardControl(this);

        public bool IsTaunted => StatusEffectSystem.TryGetForcedEnemyTarget(this, out _);

        public bool IsUnderForcedMovement => activeForcedMovement != null;

        public bool HasRecentForcedMovementInterrupt => forcedMovementInterruptTicksRemaining > 0;

        public bool IsActionLocked => actionLockRemainingSeconds > 0f;

        public bool CanMove => !IsActionLocked && !StatusEffectSystem.HasBehaviorFlag(this, StatusBehaviorFlags.BlocksMovement) && !IsUnderForcedMovement;

        public bool CanAttack => !IsActionLocked && !IsReactiveCounterBlockingBasicAttacks && !StatusEffectSystem.HasBehaviorFlag(this, StatusBehaviorFlags.BlocksBasicAttacks) && !IsUnderForcedMovement;

        public bool CanCastSkills => !IsActionLocked && !IsReactiveCounterBlockingSkillCasts && !StatusEffectSystem.HasBehaviorFlag(this, StatusBehaviorFlags.BlocksSkillCasts) && !IsUnderForcedMovement;

        public bool CanBeDirectTargeted => !StatusEffectSystem.HasBehaviorFlag(this, StatusBehaviorFlags.BlocksDirectTargeting);

        public bool CanReceiveDamage => !StatusEffectSystem.HasBehaviorFlag(this, StatusBehaviorFlags.PreventsDamage);

        public float MaxHealth => Definition != null ? StatusEffectSystem.GetModifiedStat(this, Definition.baseStats.maxHealth, StatusEffectType.MaxHealthModifier) : 0f;

        public float AttackRange
        {
            get
            {
                if (Definition == null)
                {
                    return 0f;
                }

                var baseRange = Definition.basicAttack.rangeOverride > 0f
                    ? Definition.basicAttack.rangeOverride
                    : Definition.baseStats.attackRange;
                if (activeCombatFormOverride != null && activeCombatFormOverride.AttackRangeOverride > Mathf.Epsilon)
                {
                    baseRange = activeCombatFormOverride.AttackRangeOverride;
                }

                var modifiedRange = StatusEffectSystem.GetModifiedStat(this, baseRange, StatusEffectType.AttackRangeModifier);
                if (activeCombatActionSequence == null || activeCombatActionSequence.TemporaryBasicAttackRangeOverride <= 0f)
                {
                    return modifiedRange;
                }

                return Mathf.Max(modifiedRange, activeCombatActionSequence.TemporaryBasicAttackRangeOverride);
            }
        }

        public float MoveSpeed => Definition != null ? StatusEffectSystem.GetModifiedStat(this, Definition.baseStats.moveSpeed, StatusEffectType.MoveSpeedModifier) : 0f;

        public float AttackPower
        {
            get
            {
                if (Definition == null)
                {
                    return 0f;
                }

                var totalModifierDelta = StatusEffectSystem.GetTotalMagnitude(this, StatusEffectType.AttackPowerModifier)
                    + PassiveAttackPowerBonusMultiplier
                    + CurrentCombatFormAttackPowerModifier;
                return Definition.baseStats.attackPower * Mathf.Max(0.1f, 1f + totalModifierDelta);
            }
        }

        public float Defense
        {
            get
            {
                if (Definition == null)
                {
                    return 0f;
                }

                var totalModifierDelta = StatusEffectSystem.GetTotalMagnitude(this, StatusEffectType.DefenseModifier)
                    + PassiveDefenseBonusMultiplier;
                return Definition.baseStats.defense * Mathf.Max(0.1f, 1f + totalModifierDelta);
            }
        }

        public float CriticalChance
        {
            get
            {
                if (Definition == null)
                {
                    return 0f;
                }

                return Mathf.Clamp01(StatusEffectSystem.GetModifiedStat(this, Definition.baseStats.criticalChance, StatusEffectType.CriticalChanceModifier));
            }
        }

        public float CriticalDamageMultiplier
        {
            get
            {
                if (Definition == null)
                {
                    return 1f;
                }

                return Mathf.Max(1f, StatusEffectSystem.GetModifiedStat(this, Definition.baseStats.criticalDamageMultiplier, StatusEffectType.CriticalDamageModifier));
            }
        }

        public float AttackInterval
        {
            get
            {
                if (Definition == null)
                {
                    return 1f;
                }

                var speedMultiplier =
                    StatusEffectSystem.GetMultiplier(this, StatusEffectType.AttackSpeedModifier)
                    * Mathf.Max(0.1f, 1f + PassiveAttackSpeedBonusMultiplier + SameTargetBasicAttackSpeedBonusMultiplier + CurrentCombatFormAttackSpeedModifier);
                var baseAttackSpeed = Mathf.Max(0.01f, Definition.baseStats.attackSpeed);
                return 1f / (baseAttackSpeed * speedMultiplier);
            }
        }

        public float ActiveSkillCooldownRemainingSeconds { get; private set; }

        public bool HasCastUltimate { get; private set; }

        public float NextUltimateDecisionCheckTimeSeconds { get; private set; }

        public bool HasInitializedUltimateDecisionSchedule { get; private set; }

        public float UltimateTimingNotBeforeTimeSeconds { get; private set; }

        public bool HasInitializedUltimateTimingWindow { get; private set; }

        public RuntimeHero LastThreatSource { get; private set; }

        public float LastThreatTimeSeconds { get; private set; }

        public RuntimeHero ActiveRetreatThreatSource { get; private set; }

        public bool HasActiveCombatActionSequence => activeCombatActionSequence != null;

        public RuntimeCombatActionSequence ActiveCombatActionSequence => activeCombatActionSequence;

        public bool HasPendingCombatAction => pendingCombatAction != null;

        public float PassiveAttackPowerBonusMultiplier => GetPassiveAttackPowerBonusMultiplier();

        public float PassiveAttackSpeedBonusMultiplier => GetPassiveAttackSpeedBonusMultiplier();

        public float SameTargetBasicAttackSpeedBonusMultiplier => GetSameTargetBasicAttackSpeedBonusMultiplier();

        public float PassiveDefenseBonusMultiplier => GetPassiveDefenseBonusMultiplier();

        public float PassiveLifestealRatio => GetPassiveLifestealRatio();

        public bool RejectsExternalPositiveEffects => GetRejectsExternalPositiveEffects();

        public float CurrentTemporaryOverrideLifestealRatio => GetCurrentTemporaryOverrideLifestealRatio();

        public float CurrentLifestealRatio => GetCurrentLifestealRatio();

        public float CurrentVisualScaleMultiplier => GetCurrentVisualScaleMultiplier();

        public Color CurrentVisualTintColor => GetCurrentVisualTintColor();

        public float CurrentVisualTintStrength => GetCurrentVisualTintStrength();

        public string CurrentVisualFormKey => activeCombatFormOverride != null ? activeCombatFormOverride.FormKey : string.Empty;

        public SkillData CurrentCombatFormSourceSkill => activeCombatFormOverride?.SourceSkill;

        public bool UsesProjectileBasicAttack => GetCurrentBasicAttackUsesProjectile();

        public float CurrentBasicAttackProjectileSpeed => GetCurrentBasicAttackProjectileSpeed();

        public float CurrentCombatFormAttackPowerModifier => activeCombatFormOverride != null ? activeCombatFormOverride.AttackPowerModifier : 0f;

        public float CurrentCombatFormAttackSpeedModifier => activeCombatFormOverride != null ? activeCombatFormOverride.AttackSpeedModifier : 0f;

        public SkillData CurrentTemporaryOverrideSourceSkill => GetCurrentTemporaryOverrideSourceSkill();

        public SkillData CurrentLifestealSourceSkill => GetCurrentLifestealSourceSkill();

        public bool HasActiveReactiveCounter => activeReactiveCounter != null && !activeReactiveCounter.IsExpired;

        public SkillData ActiveReactiveCounterSourceSkill => activeReactiveCounter?.SourceSkill;

        public ReactiveCounterData ActiveReactiveCounterData => activeReactiveCounter?.Definition;

        public bool IsReactiveCounterBlockingBasicAttacks => HasActiveReactiveCounter && activeReactiveCounter.BlocksBasicAttacks;

        public bool IsReactiveCounterBlockingSkillCasts => HasActiveReactiveCounter && activeReactiveCounter.BlocksSkillCasts;

        private readonly List<RuntimeStatusEffect> activeStatusEffects = new List<RuntimeStatusEffect>();
        private readonly List<RuntimeSkillTemporaryOverride> activeTemporarySkillOverrides = new List<RuntimeSkillTemporaryOverride>();
        private readonly List<RuntimeContributionRecord> recentHostileContributors = new List<RuntimeContributionRecord>();
        private readonly List<RuntimeContributionRecord> recentDirectHostileDamageContributors = new List<RuntimeContributionRecord>();
        private readonly List<RuntimeContributionRecord> recentSupportContributors = new List<RuntimeContributionRecord>();
        private readonly List<RuntimePassiveSkillState> passiveSkillStates = new List<RuntimePassiveSkillState>();
        private RuntimeForcedMovement activeForcedMovement;
        private RuntimeCombatActionSequence activeCombatActionSequence;
        private RuntimeCombatFormOverride activeCombatFormOverride;
        private RuntimeReactiveCounterStance activeReactiveCounter;
        private PendingCombatAction pendingCombatAction;
        private RuntimeHero sameTargetBasicAttackStackTarget;
        private BasicAttackSameTargetStackData activeSameTargetBasicAttackStacking;
        private int sameTargetBasicAttackStackCount;
        private float pendingActionTriggerRemainingSeconds;
        private float actionLockRemainingSeconds;
        private int forcedMovementInterruptTicksRemaining;

        public void ResetToSpawn()
        {
            StatusEffectSystem.ClearStatuses(this);
            CurrentPosition = SpawnPosition;
            CurrentHealth = MaxHealth;
            CurrentBattleTimeSeconds = 0f;
            RespawnRemainingSeconds = 0f;
            AttackCooldownRemainingSeconds = 0f;
            CurrentBasicAttackVariantIndex = GetClampedStartingBasicAttackVariantIndex();
            ClearCombatActionState();
            ClearCombatActionSequence();
            CurrentTarget = null;
            ResetSameTargetBasicAttackStacks();
            IsDead = false;
            CombatEngagedSeconds = 0f;
            NextUltimateDecisionCheckTimeSeconds = 0f;
            HasInitializedUltimateDecisionSchedule = false;
            VisualHeightOffset = 0f;
            activeForcedMovement = null;
            forcedMovementInterruptTicksRemaining = 0;
            activeTemporarySkillOverrides.Clear();
            activeCombatFormOverride = null;
            activeReactiveCounter = null;
            ResetPassiveSkillStatesForSpawn();
            ClearThreatTracking();
            ClearContributionHistory();
        }

        public float ApplyDamage(float amount, Action<RuntimeStatusEffect> onExpiredStatus = null)
        {
            if (amount <= 0f || IsDead || !CanReceiveDamage)
            {
                return 0f;
            }

            amount -= ConsumeShield(amount, onExpiredStatus);
            if (amount <= 0f)
            {
                return 0f;
            }

            return ApplyHealthLoss(amount);
        }

        public float ConsumeShield(float amount, Action<RuntimeStatusEffect> onExpiredStatus = null)
        {
            if (amount <= 0f || IsDead || !CanReceiveDamage)
            {
                return 0f;
            }

            var absorbedAmount = StatusEffectSystem.ConsumeShield(this, amount);
            StatusEffectSystem.RemoveExpiredStatuses(this, onExpiredStatus);
            return absorbedAmount;
        }

        public float ApplyHealthLoss(float amount)
        {
            if (amount <= 0f || IsDead || !CanReceiveDamage)
            {
                return 0f;
            }

            var previousHealth = CurrentHealth;
            var deathPreventFloor = StatusEffectSystem.GetDeathPreventHealthFloor(this);
            var minimumHealth = deathPreventFloor > 0f
                ? Mathf.Min(previousHealth, MaxHealth, deathPreventFloor)
                : 0f;
            CurrentHealth = Mathf.Max(minimumHealth, CurrentHealth - amount);
            return previousHealth - CurrentHealth;
        }

        public void Tick(float deltaTime, Action<RuntimeStatusEffect> onPeriodicStatusTick = null, Action<RuntimeStatusEffect> onExpiredStatus = null)
        {
            if (IsDead)
            {
                RespawnRemainingSeconds = Mathf.Max(0f, RespawnRemainingSeconds - deltaTime);
                CombatEngagedSeconds = 0f;
                return;
            }

            AttackCooldownRemainingSeconds = Mathf.Max(0f, AttackCooldownRemainingSeconds - deltaTime);
            ActiveSkillCooldownRemainingSeconds = Mathf.Max(0f, ActiveSkillCooldownRemainingSeconds - deltaTime);
            pendingActionTriggerRemainingSeconds = Mathf.Max(0f, pendingActionTriggerRemainingSeconds - deltaTime);
            actionLockRemainingSeconds = Mathf.Max(0f, actionLockRemainingSeconds - deltaTime);
            TickForcedMovement(deltaTime);
            StatusEffectSystem.Tick(this, deltaTime, onPeriodicStatusTick, onExpiredStatus);
            UpdateStatusDrivenVisualHeightOffset();
            ClampActiveSkillCooldownToStatusCap();
            TickTemporarySkillOverrides(deltaTime);
            TickCombatFormOverride(deltaTime);
            TickReactiveCounter(deltaTime);
            if (HasHardControl)
            {
                ClearCombatActionState();
            }
            else if (TryGetForcedEnemyTarget(out var forcedTarget))
            {
                InterruptCombatActionForForcedTarget(forcedTarget);
            }

            if (activeCombatActionSequence != null)
            {
                activeCombatActionSequence.Tick(this, deltaTime);
                if (activeCombatActionSequence.ShouldInterrupt(this))
                {
                    ClearCombatActionSequence();
                }
            }

            if (CurrentTarget != null && !CurrentTarget.IsDead)
            {
                CombatEngagedSeconds += deltaTime;
            }
            else
            {
                CombatEngagedSeconds = 0f;
            }

            CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
            if (forcedMovementInterruptTicksRemaining > 0)
            {
                forcedMovementInterruptTicksRemaining--;
            }
        }

        public void StartForcedMovement(Vector3 destination, float durationSeconds, float peakHeight)
        {
            destination = Stage01ArenaSpec.ClampPosition(destination);
            destination = new Vector3(destination.x, 0f, destination.z);
            RegisterForcedMovementInterrupt();
            ClearCombatActionState();

            if (durationSeconds <= Mathf.Epsilon)
            {
                CurrentPosition = destination;
                VisualHeightOffset = 0f;
                activeForcedMovement = null;
                return;
            }

            activeForcedMovement = new RuntimeForcedMovement(CurrentPosition, destination, durationSeconds, peakHeight);
        }

        public void SetTarget(RuntimeHero target)
        {
            if (CurrentTarget != target)
            {
                ResetSameTargetBasicAttackStacks();
            }

            CurrentTarget = target;
        }

        public void RecordSameTargetBasicAttackHit(RuntimeHero target, BasicAttackSameTargetStackData stacking)
        {
            if (target == null || target.IsDead || !CanUseSameTargetBasicAttackStacking(stacking))
            {
                return;
            }

            if (sameTargetBasicAttackStackTarget != target)
            {
                sameTargetBasicAttackStackTarget = target;
                sameTargetBasicAttackStackCount = 0;
            }

            activeSameTargetBasicAttackStacking = stacking;
            sameTargetBasicAttackStackCount = Mathf.Min(
                Mathf.Max(1, stacking.maxStacks),
                sameTargetBasicAttackStackCount + 1);
        }

        public void SetBattleTimeSeconds(float battleTimeSeconds)
        {
            CurrentBattleTimeSeconds = Mathf.Max(0f, battleTimeSeconds);
        }

        public void RecordThreat(RuntimeHero source, float currentTimeSeconds)
        {
            if (source == null || source == this || source.IsDead || source.Side == Side)
            {
                return;
            }

            LastThreatSource = source;
            LastThreatTimeSeconds = Mathf.Max(0f, currentTimeSeconds);
        }

        public bool TryGetRecentThreat(float currentTimeSeconds, float recentWindowSeconds, out RuntimeHero threat)
        {
            threat = LastThreatSource;
            if (threat == null || threat.IsDead || threat.Side == Side)
            {
                threat = null;
                return false;
            }

            var threatAgeSeconds = Mathf.Max(0f, currentTimeSeconds - LastThreatTimeSeconds);
            if (threatAgeSeconds > Mathf.Max(0f, recentWindowSeconds))
            {
                if (ActiveRetreatThreatSource == threat)
                {
                    ActiveRetreatThreatSource = null;
                }

                threat = null;
                return false;
            }

            return true;
        }

        public bool IsRetreatingFromThreat(RuntimeHero threat)
        {
            return threat != null && ActiveRetreatThreatSource == threat;
        }

        public void StartThreatRetreat(RuntimeHero threat)
        {
            ActiveRetreatThreatSource = threat != null && !threat.IsDead ? threat : null;
        }

        public void StopThreatRetreat()
        {
            ActiveRetreatThreatSource = null;
        }

        public void StartAttackCooldown()
        {
            AttackCooldownRemainingSeconds = AttackInterval;
        }

        public void BeginBasicAttack(
            RuntimeHero target,
            ResolvedBasicAttack basicAttack,
            float windupSeconds,
            float recoverySeconds,
            bool consumeAttackCooldown = true,
            bool isActionSequenceStep = false)
        {
            if (Definition?.basicAttack == null || target == null || basicAttack == null || IsDead)
            {
                return;
            }

            if (consumeAttackCooldown)
            {
                StartAttackCooldown();
            }

            if (basicAttack.AdvanceSequenceOnUse)
            {
                AdvanceBasicAttackVariantIndex();
            }

            StartCombatAction(
                new PendingCombatAction(
                    target,
                    basicAttack,
                    suppressActionSequenceTrigger: false,
                    isActionSequenceStep: isActionSequenceStep),
                windupSeconds,
                recoverySeconds);
        }

        public void BeginSkillCast(
            ResolvedSkillCast resolvedSkill,
            RuntimeHero primaryTarget,
            IReadOnlyList<RuntimeHero> affectedTargets,
            float windupSeconds,
            float recoverySeconds,
            bool consumeCooldown = true,
            bool suppressActionSequenceTrigger = false,
            bool isActionSequenceStep = false)
        {
            var skill = resolvedSkill?.Skill;
            if (skill == null || IsDead)
            {
                return;
            }

            if (consumeCooldown)
            {
                StartSkillCooldown(skill.slotType, skill.cooldownSeconds);
                RecordSkillCast(skill.slotType);
            }

            StartCombatAction(
                new PendingCombatAction(
                    resolvedSkill,
                    primaryTarget,
                    affectedTargets,
                    suppressActionSequenceTrigger,
                    isActionSequenceStep),
                windupSeconds,
                recoverySeconds);
        }

        public bool TryConsumeReadyCombatAction(out PendingCombatAction action)
        {
            if (pendingCombatAction == null || pendingActionTriggerRemainingSeconds > 0f)
            {
                action = null;
                return false;
            }

            action = pendingCombatAction;
            pendingCombatAction = null;
            pendingActionTriggerRemainingSeconds = 0f;
            return true;
        }

        public void MarkKill()
        {
            Kills++;
        }

        public void RecordDamage(float amount)
        {
            DamageDealt += Mathf.Max(0f, amount);
        }

        public void RecordDamageTaken(float amount)
        {
            DamageTaken += Mathf.Max(0f, amount);
        }

        public void RecordHealing(float amount)
        {
            HealingDone += Mathf.Max(0f, amount);
        }

        public void RecordShielding(float amount)
        {
            ShieldingDone += Mathf.Max(0f, amount);
        }

        public void MarkAssist()
        {
            Assists++;
        }

        public bool CanReceivePositiveEffectsFrom(RuntimeHero source)
        {
            if (source == null || source == this)
            {
                return true;
            }

            if (source.Side != Side)
            {
                return true;
            }

            return !RejectsExternalPositiveEffects;
        }

        public void ResolveKillParticipationRewards(Action<SkillData, int, int, int, float, float, float> onResolved)
        {
            ForEachPassiveSkill((skill, passiveData) =>
            {
                if (passiveData == null || !passiveData.HasKillParticipationTrigger)
                {
                    return;
                }

                var state = GetOrCreatePassiveSkillState(skill);
                var previousStackCount = state.KillParticipationStacks;
                var maxStacks = Mathf.Max(0, passiveData.killParticipationMaxStacks);
                if (state.KillParticipationStacks < maxStacks)
                {
                    state.KillParticipationStacks++;
                }

                var currentStackCount = state.KillParticipationStacks;
                var actualHeal = 0f;
                if (!IsDead && passiveData.killParticipationHealPercentMaxHealth > Mathf.Epsilon)
                {
                    actualHeal = ApplyHealing(MaxHealth * passiveData.killParticipationHealPercentMaxHealth);
                }

                onResolved?.Invoke(
                    skill,
                    previousStackCount,
                    currentStackCount,
                    maxStacks,
                    currentStackCount * Mathf.Max(0f, passiveData.killParticipationAttackPowerBonusPerStack),
                    currentStackCount * Mathf.Max(0f, passiveData.killParticipationAttackSpeedBonusPerStack),
                    actualHeal);
            });
        }

        public void ResolvePassivePeriodicSelfHeals(
            float deltaTime,
            Action<SkillData, float, float, float> onRateChanged = null,
            Action<SkillData, float, float> onHealResolved = null)
        {
            if (IsDead || deltaTime <= 0f)
            {
                return;
            }

            ForEachPassiveSkill((skill, passiveData) =>
            {
                if (skill == null || passiveData == null || !passiveData.HasPeriodicSelfHeal)
                {
                    return;
                }

                var state = GetOrCreatePassiveSkillState(skill);
                if (state == null)
                {
                    return;
                }

                var intervalSeconds = Mathf.Max(0.1f, passiveData.periodicSelfHealIntervalSeconds);
                if (!state.HasInitializedPeriodicSelfHealTimer)
                {
                    state.InitializePeriodicSelfHealTimer(intervalSeconds);
                }

                PublishPassivePeriodicSelfHealRateIfNeeded(skill, passiveData, state, intervalSeconds, onRateChanged);

                state.PeriodicSelfHealTickRemainingSeconds -= deltaTime;
                while (state.PeriodicSelfHealTickRemainingSeconds <= 0f)
                {
                    var healPercentMaxHealth = GetPassivePeriodicSelfHealPercentMaxHealth(passiveData);
                    var actualHeal = healPercentMaxHealth > Mathf.Epsilon
                        ? ApplyHealing(MaxHealth * healPercentMaxHealth)
                        : 0f;
                    if (actualHeal > 0f)
                    {
                        onHealResolved?.Invoke(skill, actualHeal, CurrentHealth);
                    }

                    state.PeriodicSelfHealTickRemainingSeconds += intervalSeconds;
                    if (state.PeriodicSelfHealTickRemainingSeconds <= 0f)
                    {
                        state.PeriodicSelfHealTickRemainingSeconds = intervalSeconds;
                    }

                    PublishPassivePeriodicSelfHealRateIfNeeded(skill, passiveData, state, intervalSeconds, onRateChanged);
                }
            });
        }

        public void RegisterHostileContribution(RuntimeHero contributor, float timeSeconds)
        {
            RegisterContribution(recentHostileContributors, contributor, timeSeconds);
        }

        public void RegisterDirectHostileDamageContribution(RuntimeHero contributor, float timeSeconds)
        {
            RegisterContribution(recentDirectHostileDamageContributors, contributor, timeSeconds);
        }

        public void RegisterSupportContribution(RuntimeHero contributor, float timeSeconds)
        {
            RegisterContribution(recentSupportContributors, contributor, timeSeconds);
        }

        public void CollectRecentHostileContributors(
            TeamSide contributorSide,
            float currentTimeSeconds,
            float recentWindowSeconds,
            ICollection<RuntimeHero> results)
        {
            CollectRecentContributors(
                recentHostileContributors,
                contributorSide,
                currentTimeSeconds,
                recentWindowSeconds,
                results);
        }

        public void CollectRecentSupportContributors(
            TeamSide contributorSide,
            float currentTimeSeconds,
            float recentWindowSeconds,
            ICollection<RuntimeHero> results)
        {
            CollectRecentContributors(
                recentSupportContributors,
                contributorSide,
                currentTimeSeconds,
                recentWindowSeconds,
                results);
        }

        public void ClearContributionHistory()
        {
            recentHostileContributors.Clear();
            recentDirectHostileDamageContributors.Clear();
            recentSupportContributors.Clear();
        }

        public void MarkDead(float respawnDelaySeconds, Action<RuntimeStatusEffect> onRemovedStatus = null)
        {
            IsDead = true;
            CurrentTarget = null;
            ResetSameTargetBasicAttackStacks();
            RespawnRemainingSeconds = Mathf.Max(0f, respawnDelaySeconds);
            CurrentHealth = 0f;
            Deaths++;
            CombatEngagedSeconds = 0f;
            ClearCombatActionState();
            ClearCombatActionSequence();
            VisualHeightOffset = 0f;
            activeForcedMovement = null;
            forcedMovementInterruptTicksRemaining = 0;
            StatusEffectSystem.ClearStatuses(this, onRemovedStatus);
            ClearThreatTracking();
            ClearContributionHistory();
            activeTemporarySkillOverrides.Clear();
            if (activeCombatFormOverride == null || activeCombatFormOverride.ExpiresOnDeath)
            {
                activeCombatFormOverride = null;
            }

            activeReactiveCounter = null;
        }

        public bool ReadyToRevive()
        {
            return IsDead && RespawnRemainingSeconds <= 0f;
        }

        public float ApplyHealing(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return 0f;
            }

            amount = Mathf.Max(0f, amount * StatusEffectSystem.GetHealTakenMultiplier(this));
            if (amount <= 0f)
            {
                return 0f;
            }

            var previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            return CurrentHealth - previousHealth;
        }

        public bool HasStatus(StatusEffectType effectType)
        {
            return StatusEffectSystem.HasStatus(this, effectType);
        }

        public bool TryGetForcedEnemyTarget(out RuntimeHero forcedTarget)
        {
            return StatusEffectSystem.TryGetForcedEnemyTarget(this, out forcedTarget);
        }

        public bool ApplyStatusEffect(StatusEffectData data, RuntimeHero source = null, SkillData sourceSkill = null)
        {
            var applied = ApplyStatusEffect(data, source, sourceSkill, source, out _);
            if (applied)
            {
                ClampActiveSkillCooldownToStatusCap();
            }

            return applied;
        }

        public bool ApplyStatusEffect(
            StatusEffectData data,
            RuntimeHero source,
            SkillData sourceSkill,
            RuntimeHero appliedBy,
            out RuntimeStatusEffect appliedStatus)
        {
            var applied = StatusEffectSystem.TryApplyStatus(this, data, source, sourceSkill, appliedBy, out appliedStatus);
            if (applied)
            {
                ClampActiveSkillCooldownToStatusCap();
            }

            return applied;
        }

        public bool CanUseActiveSkill()
        {
            return Definition != null
                && Definition.activeSkill != null
                && Definition.activeSkill.activationMode == SkillActivationMode.Active
                && ActiveSkillCooldownRemainingSeconds <= 0f
                && CanCastSkills;
        }

        public bool CanUseUltimate()
        {
            return Definition != null
                && Definition.ultimateSkill != null
                && Definition.ultimateSkill.activationMode == SkillActivationMode.Active
                && !HasCastUltimate
                && CanCastSkills;
        }

        public void StartSkillCooldown(SkillSlotType slotType, float cooldownSeconds)
        {
            if (slotType == SkillSlotType.ActiveSkill)
            {
                ActiveSkillCooldownRemainingSeconds = cooldownSeconds;
                ClampActiveSkillCooldownToStatusCap();
                return;
            }

            HasCastUltimate = true;
        }

        private void RecordSkillCast(SkillSlotType slotType)
        {
            if (slotType == SkillSlotType.ActiveSkill)
            {
                ActiveSkillCastCount++;
                return;
            }

            if (slotType == SkillSlotType.Ultimate)
            {
                UltimateCastCount++;
            }
        }

        public void ApplyTemporarySkillOverride(SkillData sourceSkill)
        {
            var definition = sourceSkill?.temporaryOverride;
            if (sourceSkill == null
                || definition == null
                || !definition.HasAnyOverride)
            {
                return;
            }

            for (var i = 0; i < activeTemporarySkillOverrides.Count; i++)
            {
                var runtimeOverride = activeTemporarySkillOverrides[i];
                if (runtimeOverride == null || runtimeOverride.SourceSkill != sourceSkill)
                {
                    continue;
                }

                runtimeOverride.Refresh(definition);
                return;
            }

            activeTemporarySkillOverrides.Add(new RuntimeSkillTemporaryOverride(sourceSkill, definition));
        }

        public bool ApplyCombatFormOverride(SkillData sourceSkill, CombatFormOverrideData definition)
        {
            if (sourceSkill == null || definition == null || !definition.HasAnyOverride)
            {
                return false;
            }

            if (activeCombatFormOverride != null && activeCombatFormOverride.SourceSkill == sourceSkill)
            {
                activeCombatFormOverride.Refresh(definition);
                return true;
            }

            activeCombatFormOverride = new RuntimeCombatFormOverride(sourceSkill, definition);
            return true;
        }

        public bool ApplyReactiveCounter(SkillData sourceSkill, ReactiveCounterData definition)
        {
            if (sourceSkill == null || definition == null || !definition.HasAnyRuntimeEffect)
            {
                return false;
            }

            ClearCombatActionState();
            if (activeReactiveCounter != null && activeReactiveCounter.SourceSkill == sourceSkill)
            {
                activeReactiveCounter.Refresh(definition);
                return true;
            }

            activeReactiveCounter = new RuntimeReactiveCounterStance(sourceSkill, definition);
            return true;
        }

        public bool TryConsumeReactiveCounterTrigger(RuntimeHero source, float currentTimeSeconds)
        {
            return activeReactiveCounter != null
                && !activeReactiveCounter.IsExpired
                && activeReactiveCounter.TryConsumeTrigger(source, currentTimeSeconds);
        }

        public void InitializeUltimateDecisionSchedule(float firstCheckTimeSeconds)
        {
            NextUltimateDecisionCheckTimeSeconds = Mathf.Max(0f, firstCheckTimeSeconds);
            HasInitializedUltimateDecisionSchedule = true;
        }

        public void ScheduleNextUltimateDecisionCheck(float nextCheckTimeSeconds)
        {
            NextUltimateDecisionCheckTimeSeconds = Mathf.Max(0f, nextCheckTimeSeconds);
            HasInitializedUltimateDecisionSchedule = true;
        }

        public void InitializeUltimateTimingWindow(float notBeforeTimeSeconds)
        {
            UltimateTimingNotBeforeTimeSeconds = Mathf.Max(0f, notBeforeTimeSeconds);
            HasInitializedUltimateTimingWindow = true;
        }

        public void StartCombatActionSequence(RuntimeCombatActionSequence sequence)
        {
            activeCombatActionSequence = sequence;
        }

        public void ResetBasicAttackVariantIndex()
        {
            CurrentBasicAttackVariantIndex = GetClampedStartingBasicAttackVariantIndex();
        }

        public void AdvanceBasicAttackVariantIndex()
        {
            var variants = Definition?.basicAttack?.variants;
            if (variants == null || variants.Count <= 0)
            {
                CurrentBasicAttackVariantIndex = 0;
                return;
            }

            CurrentBasicAttackVariantIndex = (GetClampedBasicAttackVariantIndex() + 1) % variants.Count;
        }

        public int GetClampedBasicAttackVariantIndex()
        {
            var variants = Definition?.basicAttack?.variants;
            if (variants == null || variants.Count <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(CurrentBasicAttackVariantIndex, 0, variants.Count - 1);
        }

        public void ClearCombatActionSequence()
        {
            activeCombatActionSequence = null;
        }

        private void ClearThreatTracking()
        {
            LastThreatSource = null;
            LastThreatTimeSeconds = -1f;
            ActiveRetreatThreatSource = null;
        }

        private int GetClampedStartingBasicAttackVariantIndex()
        {
            var variants = Definition?.basicAttack?.variants;
            if (variants == null || variants.Count <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(Definition.basicAttack.startingVariantIndex, 0, variants.Count - 1);
        }

        private static void RegisterContribution(
            List<RuntimeContributionRecord> records,
            RuntimeHero contributor,
            float timeSeconds)
        {
            if (records == null || contributor == null)
            {
                return;
            }

            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (record == null || record.Contributor != contributor)
                {
                    continue;
                }

                record.Refresh(timeSeconds);
                return;
            }

            records.Add(new RuntimeContributionRecord(contributor, timeSeconds));
        }

        private static void CollectRecentContributors(
            List<RuntimeContributionRecord> records,
            TeamSide contributorSide,
            float currentTimeSeconds,
            float recentWindowSeconds,
            ICollection<RuntimeHero> results)
        {
            if (records == null || results == null)
            {
                return;
            }

            var cutoffTimeSeconds = Mathf.Max(0f, currentTimeSeconds - Mathf.Max(0f, recentWindowSeconds));
            for (var i = records.Count - 1; i >= 0; i--)
            {
                var record = records[i];
                if (record?.Contributor == null)
                {
                    records.RemoveAt(i);
                    continue;
                }

                if (record.LastContributionTimeSeconds < cutoffTimeSeconds)
                {
                    records.RemoveAt(i);
                    continue;
                }

                if (contributorSide != TeamSide.None && record.Contributor.Side != contributorSide)
                {
                    continue;
                }

                results.Add(record.Contributor);
            }
        }

        private void ClearCombatActionState()
        {
            if (pendingCombatAction != null && pendingCombatAction.IsActionSequenceStep && activeCombatActionSequence != null)
            {
                activeCombatActionSequence.RestoreQueuedExecution();
            }

            pendingCombatAction = null;
            pendingActionTriggerRemainingSeconds = 0f;
            actionLockRemainingSeconds = 0f;
        }

        private void InterruptCombatActionForForcedTarget(RuntimeHero forcedTarget)
        {
            if (forcedTarget == null || pendingCombatAction == null)
            {
                return;
            }

            if (pendingCombatAction.ActionType == CombatActionType.SkillCast)
            {
                ClearCombatActionState();
                return;
            }

            if (pendingCombatAction.ActionType == CombatActionType.BasicAttack
                && pendingCombatAction.Target != forcedTarget)
            {
                ClearCombatActionState();
            }
        }

        private void StartCombatAction(PendingCombatAction action, float windupSeconds, float recoverySeconds)
        {
            pendingCombatAction = action;
            pendingActionTriggerRemainingSeconds = Mathf.Max(0f, windupSeconds);
            actionLockRemainingSeconds = pendingActionTriggerRemainingSeconds + Mathf.Max(0f, recoverySeconds);
        }

        private void TickForcedMovement(float deltaTime)
        {
            if (activeForcedMovement == null)
            {
                VisualHeightOffset = 0f;
                return;
            }

            activeForcedMovement.Tick(deltaTime);
            CurrentPosition = Stage01ArenaSpec.ClampPosition(activeForcedMovement.CurrentGroundPosition);
            CurrentPosition = new Vector3(CurrentPosition.x, 0f, CurrentPosition.z);
            VisualHeightOffset = activeForcedMovement.CurrentHeightOffset;

            if (!activeForcedMovement.IsComplete)
            {
                return;
            }

            VisualHeightOffset = 0f;
            activeForcedMovement = null;
        }

        private void UpdateStatusDrivenVisualHeightOffset()
        {
            if (activeForcedMovement != null)
            {
                return;
            }

            var statusDrivenHeight = 0f;
            for (var i = 0; i < activeStatusEffects.Count; i++)
            {
                var status = activeStatusEffects[i];
                if (status == null
                    || status.EffectType != StatusEffectType.KnockUp
                    || status.IsExpired)
                {
                    continue;
                }

                var durationSeconds = Mathf.Max(0.0001f, status.TotalDurationSeconds);
                var elapsedRatio = 1f - Mathf.Clamp01(status.RemainingDurationSeconds / durationSeconds);
                var arcHeight = Mathf.Sin(elapsedRatio * Mathf.PI) * DefaultKnockUpVisualPeakHeight;
                if (arcHeight > statusDrivenHeight)
                {
                    statusDrivenHeight = arcHeight;
                }
            }

            VisualHeightOffset = statusDrivenHeight;
        }

        private void RegisterForcedMovementInterrupt()
        {
            forcedMovementInterruptTicksRemaining = 2;
        }

        private void TickTemporarySkillOverrides(float deltaTime)
        {
            for (var i = activeTemporarySkillOverrides.Count - 1; i >= 0; i--)
            {
                var runtimeOverride = activeTemporarySkillOverrides[i];
                if (runtimeOverride == null)
                {
                    activeTemporarySkillOverrides.RemoveAt(i);
                    continue;
                }

                runtimeOverride.Tick(deltaTime);
                if (runtimeOverride.IsExpired)
                {
                    activeTemporarySkillOverrides.RemoveAt(i);
                }
            }
        }

        private void TickCombatFormOverride(float deltaTime)
        {
            if (activeCombatFormOverride == null)
            {
                return;
            }

            activeCombatFormOverride.Tick(deltaTime);
            if (activeCombatFormOverride.IsExpired)
            {
                activeCombatFormOverride = null;
            }
        }

        private void TickReactiveCounter(float deltaTime)
        {
            if (activeReactiveCounter == null)
            {
                return;
            }

            activeReactiveCounter.Tick(deltaTime);
            if (activeReactiveCounter.IsExpired)
            {
                activeReactiveCounter = null;
            }
        }

        private bool GetCurrentBasicAttackUsesProjectile()
        {
            if (activeCombatFormOverride != null && activeCombatFormOverride.OverrideUsesProjectile)
            {
                return activeCombatFormOverride.UsesProjectile;
            }

            return Definition?.basicAttack != null && Definition.basicAttack.usesProjectile;
        }

        private float GetCurrentBasicAttackProjectileSpeed()
        {
            if (activeCombatFormOverride != null && activeCombatFormOverride.ProjectileSpeedOverride > Mathf.Epsilon)
            {
                return activeCombatFormOverride.ProjectileSpeedOverride;
            }

            return Definition?.basicAttack != null ? Mathf.Max(0f, Definition.basicAttack.projectileSpeed) : 0f;
        }

        private float GetPassiveAttackPowerBonusMultiplier()
        {
            var totalBonus = 0f;
            ForEachPassiveSkill((skill, passiveData) =>
            {
                if (passiveData == null)
                {
                    return;
                }

                if (!IsDead && passiveData.HasMissingHealthAttackPowerBonus)
                {
                    var maxHealth = MaxHealth;
                    if (maxHealth > Mathf.Epsilon)
                    {
                        var missingHealthRatio = 1f - Mathf.Clamp01(CurrentHealth / maxHealth);
                        totalBonus += Mathf.Min(
                            Mathf.Max(0f, passiveData.maxAttackPowerBonus),
                            missingHealthRatio * Mathf.Max(0f, passiveData.missingHealthAttackPowerRatio));
                    }
                }

                if (!passiveData.HasKillParticipationTrigger)
                {
                    return;
                }

                var state = GetPassiveSkillState(skill);
                if (state == null || state.KillParticipationStacks <= 0)
                {
                    return;
                }

                totalBonus += state.KillParticipationStacks * Mathf.Max(0f, passiveData.killParticipationAttackPowerBonusPerStack);
            });
            return totalBonus;
        }

        private float GetPassiveDefenseBonusMultiplier()
        {
            if (IsDead)
            {
                return 0f;
            }

            var totalBonus = 0f;
            ForEachPassiveSkill((_, passiveData) =>
            {
                if (passiveData == null || !passiveData.HasRecentDirectHostileSourceDefenseBonus)
                {
                    return;
                }

                var hostileSourceCount = CountRecentDirectHostileDamageContributors(
                    passiveData.recentDirectHostileSourceWindowSeconds);
                if (hostileSourceCount <= 0)
                {
                    return;
                }

                totalBonus += Mathf.Min(
                    Mathf.Max(0f, passiveData.maxDefenseBonus),
                    hostileSourceCount * Mathf.Max(0f, passiveData.recentDirectHostileSourceDefenseBonusPerSource));
            });
            return totalBonus;
        }

        private float GetPassiveLifestealRatio()
        {
            if (IsDead)
            {
                return 0f;
            }

            var totalRatio = 0f;
            ForEachPassiveSkill((_, passiveData) =>
            {
                if (passiveData == null || !passiveData.HasLowHealthLifesteal)
                {
                    return;
                }

                var maxHealth = MaxHealth;
                if (maxHealth <= Mathf.Epsilon)
                {
                    return;
                }

                var currentHealthRatio = Mathf.Clamp01(CurrentHealth / maxHealth);
                if (currentHealthRatio >= Mathf.Clamp01(passiveData.lowHealthLifestealThreshold))
                {
                    return;
                }

                totalRatio += Mathf.Max(0f, passiveData.lowHealthLifestealRatio);
            });
            return totalRatio;
        }

        private float GetPassiveAttackSpeedBonusMultiplier()
        {
            var totalBonus = 0f;
            ForEachPassiveSkill((skill, passiveData) =>
            {
                if (passiveData == null || !passiveData.HasKillParticipationTrigger)
                {
                    return;
                }

                var state = GetPassiveSkillState(skill);
                if (state == null || state.KillParticipationStacks <= 0)
                {
                    return;
                }

                totalBonus += state.KillParticipationStacks * Mathf.Max(0f, passiveData.killParticipationAttackSpeedBonusPerStack);
            });
            return totalBonus;
        }

        private float GetSameTargetBasicAttackSpeedBonusMultiplier()
        {
            if (IsDead)
            {
                return 0f;
            }

            var stacking = activeSameTargetBasicAttackStacking ?? Definition?.basicAttack?.sameTargetStacking;
            if (!CanUseSameTargetBasicAttackStacking(stacking)
                || stacking.modifierEffectType != StatusEffectType.AttackSpeedModifier)
            {
                return 0f;
            }

            var maxStacks = Mathf.Max(1, stacking.maxStacks);
            if (stacking.fullStackOverrideStatusEffectType != StatusEffectType.None
                && StatusEffectSystem.HasStatus(this, stacking.fullStackOverrideStatusEffectType))
            {
                return maxStacks * Mathf.Max(0f, stacking.magnitudePerStack);
            }

            return Mathf.Clamp(sameTargetBasicAttackStackCount, 0, maxStacks)
                * Mathf.Max(0f, stacking.magnitudePerStack);
        }

        private void ResetSameTargetBasicAttackStacks()
        {
            sameTargetBasicAttackStackTarget = null;
            activeSameTargetBasicAttackStacking = null;
            sameTargetBasicAttackStackCount = 0;
        }

        private static bool CanUseSameTargetBasicAttackStacking(BasicAttackSameTargetStackData stacking)
        {
            return stacking != null
                && stacking.enabled
                && stacking.maxStacks > 0
                && stacking.magnitudePerStack > Mathf.Epsilon;
        }

        private bool GetRejectsExternalPositiveEffects()
        {
            var rejectsPositiveEffects = false;
            ForEachPassiveSkill((_, passiveData) =>
            {
                if (passiveData != null && passiveData.rejectExternalPositiveEffects)
                {
                    rejectsPositiveEffects = true;
                }
            });
            return rejectsPositiveEffects;
        }

        private int CountRecentDirectHostileDamageContributors(float recentWindowSeconds)
        {
            if (recentWindowSeconds <= Mathf.Epsilon)
            {
                recentDirectHostileDamageContributors.Clear();
                return 0;
            }

            var count = 0;
            var cutoffTimeSeconds = Mathf.Max(0f, CurrentBattleTimeSeconds - recentWindowSeconds);
            for (var i = recentDirectHostileDamageContributors.Count - 1; i >= 0; i--)
            {
                var record = recentDirectHostileDamageContributors[i];
                if (record?.Contributor == null
                    || record.Contributor.IsDead
                    || record.Contributor == this
                    || record.Contributor.Side == Side
                    || record.LastContributionTimeSeconds < cutoffTimeSeconds)
                {
                    recentDirectHostileDamageContributors.RemoveAt(i);
                    continue;
                }

                count++;
            }

            return count;
        }

        private float GetCurrentTemporaryOverrideLifestealRatio()
        {
            var additiveRatio = 0f;
            var minimumRatio = 0f;
            for (var i = 0; i < activeTemporarySkillOverrides.Count; i++)
            {
                var runtimeOverride = activeTemporarySkillOverrides[i];
                if (runtimeOverride == null)
                {
                    continue;
                }

                if (runtimeOverride.LifestealMode == SkillTemporaryOverrideLifestealMode.AtLeast)
                {
                    minimumRatio = Mathf.Max(minimumRatio, Mathf.Max(0f, runtimeOverride.LifestealRatio));
                    continue;
                }

                additiveRatio += Mathf.Max(0f, runtimeOverride.LifestealRatio);
            }

            return Mathf.Max(0f, additiveRatio + minimumRatio);
        }

        private float GetCurrentLifestealRatio()
        {
            var passiveRatio = PassiveLifestealRatio;
            var additiveTemporaryRatio = 0f;
            var minimumTemporaryRatio = 0f;
            for (var i = 0; i < activeTemporarySkillOverrides.Count; i++)
            {
                var runtimeOverride = activeTemporarySkillOverrides[i];
                if (runtimeOverride == null)
                {
                    continue;
                }

                if (runtimeOverride.LifestealMode == SkillTemporaryOverrideLifestealMode.AtLeast)
                {
                    minimumTemporaryRatio = Mathf.Max(minimumTemporaryRatio, Mathf.Max(0f, runtimeOverride.LifestealRatio));
                    continue;
                }

                additiveTemporaryRatio += Mathf.Max(0f, runtimeOverride.LifestealRatio);
            }

            return Mathf.Max(0f, Mathf.Max(passiveRatio, minimumTemporaryRatio) + additiveTemporaryRatio);
        }

        private float GetCurrentVisualScaleMultiplier()
        {
            var bestScaleMultiplier = 1f;
            for (var i = 0; i < activeTemporarySkillOverrides.Count; i++)
            {
                if (activeTemporarySkillOverrides[i] == null)
                {
                    continue;
                }

                bestScaleMultiplier = Mathf.Max(
                    bestScaleMultiplier,
                    Mathf.Max(1f, activeTemporarySkillOverrides[i].VisualScaleMultiplier));
            }

            return bestScaleMultiplier;
        }

        private Color GetCurrentVisualTintColor()
        {
            RuntimeSkillTemporaryOverride bestOverride = null;
            for (var i = 0; i < activeTemporarySkillOverrides.Count; i++)
            {
                var runtimeOverride = activeTemporarySkillOverrides[i];
                if (runtimeOverride == null || runtimeOverride.VisualTintStrength <= Mathf.Epsilon)
                {
                    continue;
                }

                if (bestOverride == null
                    || runtimeOverride.VisualTintStrength > bestOverride.VisualTintStrength + Mathf.Epsilon
                    || Mathf.Approximately(runtimeOverride.VisualTintStrength, bestOverride.VisualTintStrength)
                    && runtimeOverride.RemainingDurationSeconds > bestOverride.RemainingDurationSeconds)
                {
                    bestOverride = runtimeOverride;
                }
            }

            return bestOverride != null ? bestOverride.VisualTintColor : Color.white;
        }

        private float GetCurrentVisualTintStrength()
        {
            var bestTintStrength = 0f;
            for (var i = 0; i < activeTemporarySkillOverrides.Count; i++)
            {
                if (activeTemporarySkillOverrides[i] == null)
                {
                    continue;
                }

                bestTintStrength = Mathf.Max(
                    bestTintStrength,
                    Mathf.Clamp01(activeTemporarySkillOverrides[i].VisualTintStrength));
            }

            return bestTintStrength;
        }

        private SkillData GetCurrentTemporaryOverrideSourceSkill()
        {
            RuntimeSkillTemporaryOverride bestOverride = null;
            for (var i = 0; i < activeTemporarySkillOverrides.Count; i++)
            {
                var runtimeOverride = activeTemporarySkillOverrides[i];
                if (runtimeOverride == null)
                {
                    continue;
                }

                if (bestOverride == null
                    || runtimeOverride.RemainingDurationSeconds > bestOverride.RemainingDurationSeconds)
                {
                    bestOverride = runtimeOverride;
                }
            }

            return bestOverride?.SourceSkill;
        }

        private SkillData GetCurrentLifestealSourceSkill()
        {
            if (CurrentTemporaryOverrideLifestealRatio > Mathf.Epsilon)
            {
                return CurrentTemporaryOverrideSourceSkill;
            }

            return PassiveLifestealRatio > Mathf.Epsilon
                ? Definition?.activeSkill
                : null;
        }

        private float GetCurrentHealthRatio()
        {
            return MaxHealth > Mathf.Epsilon
                ? Mathf.Clamp01(CurrentHealth / MaxHealth)
                : 1f;
        }

        private float GetPassivePeriodicSelfHealPercentMaxHealth(PassiveSkillData passiveData)
        {
            if (passiveData == null || !passiveData.HasPeriodicSelfHeal)
            {
                return 0f;
            }

            return passiveData.ResolvePeriodicSelfHealPercentMaxHealth(GetCurrentHealthRatio());
        }

        private void PublishPassivePeriodicSelfHealRateIfNeeded(
            SkillData skill,
            PassiveSkillData passiveData,
            RuntimePassiveSkillState state,
            float intervalSeconds,
            Action<SkillData, float, float, float> onRateChanged)
        {
            if (state == null || passiveData == null || onRateChanged == null)
            {
                return;
            }

            var healPercentMaxHealth = GetPassivePeriodicSelfHealPercentMaxHealth(passiveData);
            if (Mathf.Approximately(state.LastReportedPeriodicSelfHealPercentMaxHealth, healPercentMaxHealth))
            {
                return;
            }

            state.LastReportedPeriodicSelfHealPercentMaxHealth = healPercentMaxHealth;
            onRateChanged(skill, GetCurrentHealthRatio(), healPercentMaxHealth, intervalSeconds);
        }

        private void ForEachPassiveSkill(Action<SkillData, PassiveSkillData> visitor)
        {
            if (visitor == null)
            {
                return;
            }

            var activeSkill = Definition?.activeSkill;
            if (activeSkill != null
                && activeSkill.activationMode == SkillActivationMode.Passive
                && activeSkill.passiveSkill != null)
            {
                visitor(activeSkill, activeSkill.passiveSkill);
            }

            var ultimateSkill = Definition?.ultimateSkill;
            if (ultimateSkill != null
                && ultimateSkill != activeSkill
                && ultimateSkill.activationMode == SkillActivationMode.Passive
                && ultimateSkill.passiveSkill != null)
            {
                visitor(ultimateSkill, ultimateSkill.passiveSkill);
            }
        }

        private RuntimePassiveSkillState GetPassiveSkillState(SkillData sourceSkill)
        {
            if (sourceSkill == null)
            {
                return null;
            }

            for (var i = 0; i < passiveSkillStates.Count; i++)
            {
                var state = passiveSkillStates[i];
                if (state != null && state.SourceSkill == sourceSkill)
                {
                    return state;
                }
            }

            return null;
        }

        private RuntimePassiveSkillState GetOrCreatePassiveSkillState(SkillData sourceSkill)
        {
            if (sourceSkill == null)
            {
                return null;
            }

            var existingState = GetPassiveSkillState(sourceSkill);
            if (existingState != null)
            {
                return existingState;
            }

            var state = new RuntimePassiveSkillState(sourceSkill);
            passiveSkillStates.Add(state);
            return state;
        }

        private void ResetPassiveSkillStatesForSpawn()
        {
            for (var i = 0; i < passiveSkillStates.Count; i++)
            {
                passiveSkillStates[i]?.ResetForSpawn();
            }
        }

        private void ClampActiveSkillCooldownToStatusCap()
        {
            var cooldownCap = StatusEffectSystem.GetActiveSkillCooldownCap(this);
            if (cooldownCap <= 0f)
            {
                return;
            }

            ActiveSkillCooldownRemainingSeconds = Mathf.Min(ActiveSkillCooldownRemainingSeconds, cooldownCap);
        }
    }
}
