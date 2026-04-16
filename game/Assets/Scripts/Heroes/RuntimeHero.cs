using System.Collections.Generic;
using System;
using Fight.Data;
using UnityEngine;

namespace Fight.Heroes
{
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

        public float VisualHeightOffset { get; private set; }

        public bool HasHardControl => HasStatusFlag(StatusBehaviorFlags.BlocksMovement)
            || HasStatusFlag(StatusBehaviorFlags.BlocksBasicAttacks)
            || HasStatusFlag(StatusBehaviorFlags.BlocksSkillCasts);

        public bool IsUnderForcedMovement => activeForcedMovement != null;

        public bool CanMove => !HasStatusFlag(StatusBehaviorFlags.BlocksMovement) && !IsUnderForcedMovement;

        public bool CanAttack => !HasStatusFlag(StatusBehaviorFlags.BlocksBasicAttacks) && !IsUnderForcedMovement;

        public bool CanCastSkills => !HasStatusFlag(StatusBehaviorFlags.BlocksSkillCasts) && !IsUnderForcedMovement;

        public bool CanBeDirectTargeted => !HasStatusFlag(StatusBehaviorFlags.BlocksDirectTargeting);

        public bool CanReceiveDamage => !HasStatusFlag(StatusBehaviorFlags.PreventsDamage);

        public float MaxHealth => Definition != null ? GetModifiedStat(Definition.baseStats.maxHealth, StatusEffectType.MaxHealthModifier) : 0f;

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
                return GetModifiedStat(baseRange, StatusEffectType.AttackRangeModifier);
            }
        }

        public float MoveSpeed => Definition != null ? GetModifiedStat(Definition.baseStats.moveSpeed, StatusEffectType.MoveSpeedModifier) : 0f;

        public float AttackPower => Definition != null ? GetModifiedStat(Definition.baseStats.attackPower, StatusEffectType.AttackPowerModifier) : 0f;

        public float Defense => Definition != null ? GetModifiedStat(Definition.baseStats.defense, StatusEffectType.DefenseModifier) : 0f;

        public float CriticalChance
        {
            get
            {
                if (Definition == null)
                {
                    return 0f;
                }

                return Mathf.Clamp01(GetModifiedStat(Definition.baseStats.criticalChance, StatusEffectType.CriticalChanceModifier));
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

                return Mathf.Max(1f, GetModifiedStat(Definition.baseStats.criticalDamageMultiplier, StatusEffectType.CriticalDamageModifier));
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

                var speedMultiplier = GetMultiplier(StatusEffectType.AttackSpeedModifier);
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

        private readonly List<RuntimeStatusEffect> activeStatusEffects = new List<RuntimeStatusEffect>();
        private RuntimeForcedMovement activeForcedMovement;

        public void ResetToSpawn()
        {
            CurrentPosition = SpawnPosition;
            CurrentHealth = MaxHealth;
            RespawnRemainingSeconds = 0f;
            AttackCooldownRemainingSeconds = 0f;
            CurrentTarget = null;
            IsDead = false;
            CombatEngagedSeconds = 0f;
            NextUltimateDecisionCheckTimeSeconds = 0f;
            HasInitializedUltimateDecisionSchedule = false;
            VisualHeightOffset = 0f;
            activeForcedMovement = null;
            activeStatusEffects.Clear();
            ClearThreatTracking();
        }

        public float ApplyDamage(float amount, Action<RuntimeStatusEffect> onExpiredStatus = null)
        {
            if (amount <= 0f || IsDead || !CanReceiveDamage)
            {
                return 0f;
            }

            amount -= ConsumeShield(amount);
            RemoveExpiredStatuses(onExpiredStatus);
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
            TickForcedMovement(deltaTime);

            var statusSnapshot = activeStatusEffects.ToArray();
            for (var i = statusSnapshot.Length - 1; i >= 0; i--)
            {
                var status = statusSnapshot[i];
                if (status == null || !activeStatusEffects.Contains(status))
                {
                    continue;
                }

                status.Tick(deltaTime);

                var pendingTickCount = status.ConsumePendingTickCount();
                for (var tickIndex = 0; tickIndex < pendingTickCount; tickIndex++)
                {
                    onPeriodicStatusTick?.Invoke(status);
                    if (IsDead)
                    {
                        break;
                    }
                }

                if (IsDead)
                {
                    break;
                }

                if (status.IsExpired)
                {
                    onExpiredStatus?.Invoke(status);
                    activeStatusEffects.Remove(status);
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

        public void MarkDead(float respawnDelaySeconds)
        {
            IsDead = true;
            CurrentTarget = null;
            RespawnRemainingSeconds = Mathf.Max(0f, respawnDelaySeconds);
            CurrentHealth = 0f;
            Deaths++;
            CombatEngagedSeconds = 0f;
            VisualHeightOffset = 0f;
            activeForcedMovement = null;
            activeStatusEffects.Clear();
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
            for (var i = 0; i < activeStatusEffects.Count; i++)
            {
                if (activeStatusEffects[i].EffectType == effectType)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ApplyStatusEffect(StatusEffectData data, RuntimeHero source = null, SkillData sourceSkill = null)
        {
            if (data == null || data.effectType == StatusEffectType.None)
            {
                return false;
            }

            var sameTypeCount = 0;
            RuntimeStatusEffect refreshTarget = null;

            for (var i = 0; i < activeStatusEffects.Count; i++)
            {
                if (activeStatusEffects[i].EffectType != data.effectType)
                {
                    continue;
                }

                sameTypeCount++;
                refreshTarget ??= activeStatusEffects[i];
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

            activeStatusEffects.Add(new RuntimeStatusEffect(data, source, sourceSkill));
            return true;
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

        private float GetModifiedStat(float baseValue, StatusEffectType effectType)
        {
            return baseValue * GetMultiplier(effectType);
        }

        private float ConsumeShield(float damageAmount)
        {
            var remainingDamage = Mathf.Max(0f, damageAmount);
            var absorbedDamage = 0f;

            for (var i = 0; i < activeStatusEffects.Count && remainingDamage > 0f; i++)
            {
                var status = activeStatusEffects[i];
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

        private void ClearThreatTracking()
        {
            LastThreatSource = null;
            LastThreatTimeSeconds = -1f;
            ActiveRetreatThreatSource = null;
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

        private void RemoveExpiredStatuses(Action<RuntimeStatusEffect> onExpiredStatus)
        {
            for (var i = activeStatusEffects.Count - 1; i >= 0; i--)
            {
                var status = activeStatusEffects[i];
                if (!status.IsExpired)
                {
                    continue;
                }

                onExpiredStatus?.Invoke(status);
                activeStatusEffects.RemoveAt(i);
            }
        }

        private bool HasStatusFlag(StatusBehaviorFlags flag)
        {
            for (var i = 0; i < activeStatusEffects.Count; i++)
            {
                if ((activeStatusEffects[i].Definition.BehaviorFlags & flag) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private float GetMultiplier(StatusEffectType effectType)
        {
            var multiplier = 1f;
            for (var i = 0; i < activeStatusEffects.Count; i++)
            {
                if (activeStatusEffects[i].EffectType != effectType)
                {
                    continue;
                }

                multiplier += activeStatusEffects[i].Magnitude;
            }

            return Mathf.Max(0.1f, multiplier);
        }
    }
}
