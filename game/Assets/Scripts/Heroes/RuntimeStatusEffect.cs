using Fight.Data;

namespace Fight.Heroes
{
    public class RuntimeStatusEffect
    {
        public RuntimeStatusEffect(StatusEffectData data)
        {
            EffectType = data.effectType;
            RemainingDurationSeconds = data.durationSeconds;
            Magnitude = data.magnitude;
            MaxStacks = data.maxStacks;
            RefreshDurationOnReapply = data.refreshDurationOnReapply;
        }

        public StatusEffectType EffectType { get; }

        public float RemainingDurationSeconds { get; private set; }

        public float Magnitude { get; }

        public int MaxStacks { get; }

        public bool RefreshDurationOnReapply { get; }

        public void Tick(float deltaTime)
        {
            RemainingDurationSeconds -= deltaTime;
        }

        public void Refresh(float durationSeconds)
        {
            RemainingDurationSeconds = durationSeconds;
        }

        public bool IsExpired => RemainingDurationSeconds <= 0f;
    }
}
