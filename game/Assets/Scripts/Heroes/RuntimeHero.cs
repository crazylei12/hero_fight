using System;
using System.Collections.Generic;
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
        public PendingCombatAction(RuntimeHero target, bool suppressActionSequenceTrigger = false)
        {
            ActionType = CombatActionType.BasicAttack;
            Target = target;
            AffectedTargets = Array.Empty<RuntimeHero>();
            SuppressActionSequenceTrigger = suppressActionSequenceTrigger;
        }

        public PendingCombatAction(
            SkillData skill,
            RuntimeHero primaryTarget,
            IReadOnlyList<RuntimeHero> affectedTargets,
            bool suppressActionSequenceTrigger = false)
        {
            ActionType = CombatActionType.SkillCast;
            Skill = skill;
            PrimaryTarget = primaryTarget;
            SuppressActionSequenceTrigger = suppressActionSequenceTrigger;
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

        public SkillData Skill { get; }

        public RuntimeHero PrimaryTarget { get; }

        public IReadOnlyList<RuntimeHero> AffectedTargets { get; }

        public bool SuppressActionSequenceTrigger { get; }
    }

    public class RuntimeHero
    {
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

        public int Kills { get; private set; }

        public int Deaths { get; private set; }

        public float DamageDealt { get; private set; }

        public float HealingDone { get; private set; }

        public bool IsDead { get; private set; }

        public float CombatEngagedSeconds { get; private set; }

        public IReadOnlyList<RuntimeStatusEffect> ActiveStatusEffects => activeStatusEffects;

        internal List<RuntimeStatusEffect> MutableStatusEffects => activeStatusEffects;

        public float VisualHeightOffset { get; private set; }

        public bool HasHardControl => StatusEffectSystem.HasHardControl(this);

        public bool IsUnderForcedMovement => activeForcedMovement != null;

        public bool IsActionLocked => actionLockRemainingSeconds > 0f;

        public bool CanMove => !IsActionLocked && !StatusEffectSystem.HasBehaviorFlag(this, StatusBehaviorFlags.BlocksMovement) && !IsUnderForcedMovement;

        public bool CanAttack => !IsActionLocked && !StatusEffectSystem.HasBehaviorFlag(this, StatusBehaviorFlags.BlocksBasicAttacks) && !IsUnderForcedMovement;

        public bool CanCastSkills => !IsActionLocked && !StatusEffectSystem.HasBehaviorFlag(this, StatusBehaviorFlags.BlocksSkillCasts) && !IsUnderForcedMovement;

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
                var modifiedRange = StatusEffectSystem.GetModifiedStat(this, baseRange, StatusEffectType.AttackRangeModifier);
                if (activeCombatActionSequence == null || activeCombatActionSequence.TemporaryBasicAttackRangeOverride <= 0f)
                {
                    return modifiedRange;
                }

                return Mathf.Max(modifiedRange, activeCombatActionSequence.TemporaryBasicAttackRangeOverride);
            }
        }

        public float MoveSpeed => Definition != null ? StatusEffectSystem.GetModifiedStat(this, Definition.baseStats.moveSpeed, StatusEffectType.MoveSpeedModifier) : 0f;

        public float AttackPower => Definition != null ? StatusEffectSystem.GetModifiedStat(this, Definition.baseStats.attackPower, StatusEffectType.AttackPowerModifier) : 0f;

        public float Defense => Definition != null ? StatusEffectSystem.GetModifiedStat(this, Definition.baseStats.defense, StatusEffectType.DefenseModifier) : 0f;

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

                var speedMultiplier = StatusEffectSystem.GetMultiplier(this, StatusEffectType.AttackSpeedModifier);
                var baseAttackSpeed = Mathf.Max(0.01f, Definition.baseStats.attackSpeed);
                return 1f / (baseAttackSpeed * speedMultiplier);
            }
        }

        public float ActiveSkillCooldownRemainingSeconds { get; private set; }

        public bool HasCastUltimate { get; private set; }

        public float NextUltimateDecisionCheckTimeSeconds { get; private set; }

        public bool HasInitializedUltimateDecisionSchedule { get; private set; }

        public RuntimeHero LastThreatSource { get; private set; }

        public float LastThreatTimeSeconds { get; private set; }

        public RuntimeHero ActiveRetreatThreatSource { get; private set; }

        public bool HasActiveCombatActionSequence => activeCombatActionSequence != null;

        public RuntimeCombatActionSequence ActiveCombatActionSequence => activeCombatActionSequence;

        private readonly List<RuntimeStatusEffect> activeStatusEffects = new List<RuntimeStatusEffect>();
        private RuntimeForcedMovement activeForcedMovement;
        private RuntimeCombatActionSequence activeCombatActionSequence;
        private PendingCombatAction pendingCombatAction;
        private float pendingActionTriggerRemainingSeconds;
        private float actionLockRemainingSeconds;

        public void ResetToSpawn()
        {
            StatusEffectSystem.ClearStatuses(this);
            CurrentPosition = SpawnPosition;
            CurrentHealth = MaxHealth;
            RespawnRemainingSeconds = 0f;
            AttackCooldownRemainingSeconds = 0f;
            ClearCombatActionState();
            ClearCombatActionSequence();
            CurrentTarget = null;
            IsDead = false;
            CombatEngagedSeconds = 0f;
            NextUltimateDecisionCheckTimeSeconds = 0f;
            HasInitializedUltimateDecisionSchedule = false;
            VisualHeightOffset = 0f;
            activeForcedMovement = null;
            ClearThreatTracking();
        }

        public float ApplyDamage(float amount, Action<RuntimeStatusEffect> onExpiredStatus = null)
        {
            if (amount <= 0f || IsDead || !CanReceiveDamage)
            {
                return 0f;
            }

            amount -= StatusEffectSystem.ConsumeShield(this, amount);
            StatusEffectSystem.RemoveExpiredStatuses(this, onExpiredStatus);
            if (amount <= 0f)
            {
                return 0f;
            }

            var previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
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
            if (HasHardControl)
            {
                ClearCombatActionState();
            }

            if (activeCombatActionSequence != null)
            {
                activeCombatActionSequence.Tick(deltaTime);
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
        }

        public void StartForcedMovement(Vector3 destination, float durationSeconds, float peakHeight)
        {
            destination = Stage01ArenaSpec.ClampPosition(destination);
            destination = new Vector3(destination.x, 0f, destination.z);

            if (durationSeconds <= Mathf.Epsilon)
            {
                CurrentPosition = destination;
                VisualHeightOffset = 0f;
                activeForcedMovement = null;
                return;
            }

            ClearCombatActionState();
            activeForcedMovement = new RuntimeForcedMovement(CurrentPosition, destination, durationSeconds, peakHeight);
        }

        public void SetTarget(RuntimeHero target)
        {
            CurrentTarget = target;
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

        public void BeginBasicAttack(RuntimeHero target, float windupSeconds, float recoverySeconds, bool consumeAttackCooldown = true)
        {
            if (Definition?.basicAttack == null || target == null || IsDead)
            {
                return;
            }

            if (consumeAttackCooldown)
            {
                StartAttackCooldown();
            }

            StartCombatAction(new PendingCombatAction(target), windupSeconds, recoverySeconds);
        }

        public void BeginSkillCast(
            SkillData skill,
            RuntimeHero primaryTarget,
            IReadOnlyList<RuntimeHero> affectedTargets,
            float windupSeconds,
            float recoverySeconds,
            bool consumeCooldown = true,
            bool suppressActionSequenceTrigger = false)
        {
            if (skill == null || IsDead)
            {
                return;
            }

            if (consumeCooldown)
            {
                StartSkillCooldown(skill.slotType, skill.cooldownSeconds);
            }

            StartCombatAction(
                new PendingCombatAction(skill, primaryTarget, affectedTargets, suppressActionSequenceTrigger),
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

        public void RecordHealing(float amount)
        {
            HealingDone += Mathf.Max(0f, amount);
        }

        public void MarkDead(float respawnDelaySeconds, Action<RuntimeStatusEffect> onRemovedStatus = null)
        {
            IsDead = true;
            CurrentTarget = null;
            RespawnRemainingSeconds = Mathf.Max(0f, respawnDelaySeconds);
            CurrentHealth = 0f;
            Deaths++;
            CombatEngagedSeconds = 0f;
            ClearCombatActionState();
            ClearCombatActionSequence();
            VisualHeightOffset = 0f;
            activeForcedMovement = null;
            StatusEffectSystem.ClearStatuses(this, onRemovedStatus);
            ClearThreatTracking();
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

            var previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            return CurrentHealth - previousHealth;
        }

        public bool HasStatus(StatusEffectType effectType)
        {
            return StatusEffectSystem.HasStatus(this, effectType);
        }

        public bool ApplyStatusEffect(StatusEffectData data, RuntimeHero source = null, SkillData sourceSkill = null)
        {
            return StatusEffectSystem.TryApplyStatus(this, data, source, sourceSkill);
        }

        public bool CanUseActiveSkill()
        {
            return Definition != null && Definition.activeSkill != null && ActiveSkillCooldownRemainingSeconds <= 0f && CanCastSkills;
        }

        public bool CanUseUltimate()
        {
            return Definition != null && Definition.ultimateSkill != null && !HasCastUltimate && CanCastSkills;
        }

        public void StartSkillCooldown(SkillSlotType slotType, float cooldownSeconds)
        {
            if (slotType == SkillSlotType.ActiveSkill)
            {
                ActiveSkillCooldownRemainingSeconds = cooldownSeconds;
                return;
            }

            HasCastUltimate = true;
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

        public void StartCombatActionSequence(RuntimeCombatActionSequence sequence)
        {
            activeCombatActionSequence = sequence;
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

        private void ClearCombatActionState()
        {
            pendingCombatAction = null;
            pendingActionTriggerRemainingSeconds = 0f;
            actionLockRemainingSeconds = 0f;
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
    }
}
