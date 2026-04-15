using Fight.Data;
using UnityEngine;

namespace Fight.Heroes
{
    public class RuntimeStatusEffect
    {
        private int pendingTickCount;

        public RuntimeStatusEffect(StatusEffectData data, RuntimeHero source = null, SkillData sourceSkill = null)
        {
            EffectType = data.effectType;
            Definition = StatusEffectCatalog.Get(data.effectType);
            RemainingDurationSeconds = data.durationSeconds;
            Magnitude = data.magnitude;
            TickIntervalSeconds = Mathf.Max(0.1f, data.tickIntervalSeconds);
            TimeUntilNextTickSeconds = 0f;
            MaxStacks = data.maxStacks;
            RefreshDurationOnReapply = data.refreshDurationOnReapply;
            Source = source;
            SourceSkill = sourceSkill;
        }

        public StatusEffectType EffectType { get; }

        public StatusEffectDefinition Definition { get; }

        public float RemainingDurationSeconds { get; private set; }

        public float Magnitude { get; }

        public float TickIntervalSeconds { get; private set; }

        public float TimeUntilNextTickSeconds { get; private set; }

        public int MaxStacks { get; }

        public bool RefreshDurationOnReapply { get; }

        public RuntimeHero Source { get; }

        public SkillData SourceSkill { get; }

        public void Tick(float deltaTime)
        {
            RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - deltaTime);

            if (!Definition.IsPeriodic)
            {
                return;
            }

            TimeUntilNextTickSeconds -= deltaTime;
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

        public void Refresh(StatusEffectData data)
        {
            RemainingDurationSeconds = data.durationSeconds;
            TickIntervalSeconds = Mathf.Max(0.1f, data.tickIntervalSeconds);
            TimeUntilNextTickSeconds = 0f;
        }

        public bool IsExpired => RemainingDurationSeconds <= 0f;
    }
}
