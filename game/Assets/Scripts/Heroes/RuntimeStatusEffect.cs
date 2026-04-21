using Fight.Core;
using Fight.Data;
using UnityEngine;

namespace Fight.Heroes
{
    public class RuntimeStatusEffect
    {
        private int pendingTickCount;

        public RuntimeStatusEffect(StatusEffectData data, RuntimeHero target, RuntimeHero source = null, SkillData sourceSkill = null, RuntimeHero appliedBy = null)
        {
            EffectType = data.effectType;
            Definition = StatusEffectCatalog.Get(data.effectType);
            TotalDurationSeconds = Mathf.Max(0f, data.durationSeconds);
            RemainingDurationSeconds = TotalDurationSeconds;
            BaseMagnitude = data.magnitude;
            SourceAttackPowerMultiplier = Mathf.Max(0f, data.sourceAttackPowerMultiplier);
            StackGroupKey = data.stackGroupKey ?? string.Empty;
            StatusThemeKey = data.statusThemeKey ?? string.Empty;
            Magnitude = ResolveMagnitude(data, target, source ?? appliedBy);
            ActiveSkillCooldownCapSeconds = Mathf.Max(0f, data.activeSkillCooldownCapSeconds);
            TickIntervalSeconds = Mathf.Max(0.1f, data.tickIntervalSeconds);
            TimeUntilNextTickSeconds = TickIntervalSeconds;
            MaxStacks = data.maxStacks;
            RefreshDurationOnReapply = data.refreshDurationOnReapply;
            Source = source;
            SourceSkill = sourceSkill;
            AppliedBy = appliedBy ?? source;
        }

        public StatusEffectType EffectType { get; }

        public StatusEffectDefinition Definition { get; }

        public float RemainingDurationSeconds { get; private set; }

        public float TotalDurationSeconds { get; private set; }

        public float Magnitude { get; private set; }

        public float BaseMagnitude { get; private set; }

        public float SourceAttackPowerMultiplier { get; private set; }

        public float ActiveSkillCooldownCapSeconds { get; private set; }

        public float TickIntervalSeconds { get; private set; }

        public float TimeUntilNextTickSeconds { get; private set; }

        public int MaxStacks { get; }

        public bool RefreshDurationOnReapply { get; }

        public string StackGroupKey { get; private set; }

        public string StatusThemeKey { get; private set; }

        public RuntimeHero Source { get; private set; }

        public SkillData SourceSkill { get; private set; }

        public RuntimeHero AppliedBy { get; private set; }

        public void Tick(float deltaTime)
        {
            var elapsedTime = Mathf.Min(deltaTime, RemainingDurationSeconds);
            RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - deltaTime);

            if (!Definition.IsPeriodic || elapsedTime <= 0f)
            {
                return;
            }

            TimeUntilNextTickSeconds -= elapsedTime;
            while (TimeUntilNextTickSeconds <= 0f && TickIntervalSeconds > 0f)
            {
                pendingTickCount++;
                TimeUntilNextTickSeconds += TickIntervalSeconds;
            }
        }

        public int ConsumePendingTickCount()
        {
            var result = pendingTickCount;
            pendingTickCount = 0;
            return result;
        }

        public void Refresh(StatusEffectData data, RuntimeHero target, RuntimeHero source, SkillData sourceSkill, RuntimeHero appliedBy, bool refreshMagnitude = true)
        {
            var previousTickIntervalSeconds = TickIntervalSeconds;
            var previousTimeUntilNextTickSeconds = TimeUntilNextTickSeconds;
            TotalDurationSeconds = Mathf.Max(0f, data.durationSeconds);
            RemainingDurationSeconds = TotalDurationSeconds;
            BaseMagnitude = data.magnitude;
            SourceAttackPowerMultiplier = Mathf.Max(0f, data.sourceAttackPowerMultiplier);
            StackGroupKey = data.stackGroupKey ?? string.Empty;
            StatusThemeKey = data.statusThemeKey ?? string.Empty;
            if (refreshMagnitude)
            {
                Magnitude = ResolveMagnitude(data, target, source ?? appliedBy);
            }

            ActiveSkillCooldownCapSeconds = Mathf.Max(0f, data.activeSkillCooldownCapSeconds);
            TickIntervalSeconds = Mathf.Max(0.1f, data.tickIntervalSeconds);
            Source = source;
            SourceSkill = sourceSkill;
            AppliedBy = appliedBy ?? source;

            if (!Definition.IsPeriodic)
            {
                TimeUntilNextTickSeconds = TickIntervalSeconds;
                return;
            }

            if (previousTickIntervalSeconds <= Mathf.Epsilon)
            {
                TimeUntilNextTickSeconds = TickIntervalSeconds;
                return;
            }

            var elapsedTickProgressSeconds = Mathf.Clamp(
                previousTickIntervalSeconds - previousTimeUntilNextTickSeconds,
                0f,
                previousTickIntervalSeconds);
            var normalizedProgress = elapsedTickProgressSeconds / previousTickIntervalSeconds;
            TimeUntilNextTickSeconds = Mathf.Max(0.0001f, TickIntervalSeconds * (1f - Mathf.Clamp01(normalizedProgress)));
        }

        public void ExpireImmediately()
        {
            RemainingDurationSeconds = 0f;
            TimeUntilNextTickSeconds = 0f;
        }

        public float ConsumeMagnitude(float amount)
        {
            if (amount <= 0f || Magnitude <= 0f)
            {
                return 0f;
            }

            var consumed = Mathf.Min(Magnitude, amount);
            Magnitude -= consumed;
            if (Magnitude <= 0f)
            {
                Magnitude = 0f;
                RemainingDurationSeconds = 0f;
            }

            return consumed;
        }

        public bool IsExpired => RemainingDurationSeconds <= 0f;

        private static float ResolveMagnitude(StatusEffectData data, RuntimeHero target, RuntimeHero snapshotSource)
        {
            if (data == null)
            {
                return 0f;
            }

            var baseMagnitude = data.magnitude;
            var attackPowerMultiplier = Mathf.Max(0f, data.sourceAttackPowerMultiplier);
            if (attackPowerMultiplier <= Mathf.Epsilon || snapshotSource == null)
            {
                return baseMagnitude;
            }

            if (data.effectType == StatusEffectType.DamageOverTime)
            {
                var targetDefense = target != null ? target.Defense : 0f;
                return baseMagnitude + DamageResolver.ResolveDamage(
                    snapshotSource.AttackPower,
                    0f,
                    1f,
                    targetDefense,
                    randomService: null,
                    attackPowerMultiplier);
            }

            return baseMagnitude + attackPowerMultiplier * Mathf.Max(0f, snapshotSource.AttackPower);
        }
    }
}
