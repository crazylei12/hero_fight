using System.Collections.Generic;
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

        public float UltimateCooldownRemainingSeconds { get; private set; }

        public bool HasCastUltimate { get; private set; }

        public float NextUltimateDecisionCheckTimeSeconds { get; private set; }

        public bool HasInitializedUltimateDecisionSchedule { get; private set; }

        private readonly List<RuntimeStatusEffect> activeStatusEffects = new List<RuntimeStatusEffect>();

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
            activeStatusEffects.Clear();
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        }

        public void Tick(float deltaTime)
        {
            if (IsDead)
            {
                RespawnRemainingSeconds = Mathf.Max(0f, RespawnRemainingSeconds - deltaTime);
                CombatEngagedSeconds = 0f;
                return;
            }

            AttackCooldownRemainingSeconds = Mathf.Max(0f, AttackCooldownRemainingSeconds - deltaTime);
            ActiveSkillCooldownRemainingSeconds = Mathf.Max(0f, ActiveSkillCooldownRemainingSeconds - deltaTime);
            UltimateCooldownRemainingSeconds = Mathf.Max(0f, UltimateCooldownRemainingSeconds - deltaTime);

            for (var i = activeStatusEffects.Count - 1; i >= 0; i--)
            {
                activeStatusEffects[i].Tick(deltaTime);
                if (activeStatusEffects[i].EffectType == StatusEffectType.HealOverTime)
                {
                    ApplyHealing(activeStatusEffects[i].Magnitude * deltaTime);
                }

                if (activeStatusEffects[i].IsExpired)
                {
                    activeStatusEffects.RemoveAt(i);
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

        public void SetTarget(RuntimeHero target)
        {
            CurrentTarget = target;
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
            activeStatusEffects.Clear();
        }

        public bool ReadyToRevive()
        {
            return IsDead && RespawnRemainingSeconds <= 0f;
        }

        public void ApplyHealing(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
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

        public void ApplyStatusEffect(StatusEffectData data)
        {
            if (data == null || data.effectType == StatusEffectType.None)
            {
                return;
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
                    refreshTarget.Refresh(data.durationSeconds);
                }

                return;
            }

            activeStatusEffects.Add(new RuntimeStatusEffect(data));
        }

        public bool CanUseActiveSkill()
        {
            return Definition != null && Definition.activeSkill != null && ActiveSkillCooldownRemainingSeconds <= 0f;
        }

        public bool CanUseUltimate()
        {
            return Definition != null && Definition.ultimateSkill != null && !HasCastUltimate && UltimateCooldownRemainingSeconds <= 0f;
        }

        public void StartSkillCooldown(SkillSlotType slotType, float cooldownSeconds)
        {
            if (slotType == SkillSlotType.ActiveSkill)
            {
                ActiveSkillCooldownRemainingSeconds = cooldownSeconds;
                return;
            }

            HasCastUltimate = true;
            UltimateCooldownRemainingSeconds = cooldownSeconds;
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
