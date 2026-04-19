using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public sealed class RuntimeReactiveGuard
    {
        public RuntimeReactiveGuard(RuntimeHero caster, RuntimeHero protectedHero, SkillData sourceSkill, ReactiveGuardData data)
        {
            Caster = caster;
            ProtectedHero = protectedHero;
            SourceSkill = sourceSkill;
            RemainingDurationSeconds = Mathf.Max(0f, data != null ? data.durationSeconds : 0f);
            TriggerRadius = Mathf.Max(0f, data != null ? data.triggerRadius : 0f);
            EffectRadius = Mathf.Max(0f, data != null ? data.effectRadius : 0f);
            ForcedMovementDistance = Mathf.Max(0f, data != null ? data.forcedMovementDistance : 0f);
            ForcedMovementDurationSeconds = Mathf.Max(0f, data != null ? data.forcedMovementDurationSeconds : 0f);
            ForcedMovementPeakHeight = Mathf.Max(0f, data != null ? data.forcedMovementPeakHeight : 0f);
            HealProtectedHeroPerSuccessfulKnockUp = Mathf.Max(0f, data != null ? data.healProtectedHeroPerSuccessfulKnockUp : 0f);
            TriggersRemaining = Mathf.Max(1, data != null ? data.maxTriggerCount : 1);
            OnTriggerStatusEffects = data?.onTriggerStatusEffects;
        }

        public RuntimeHero Caster { get; }

        public RuntimeHero ProtectedHero { get; }

        public SkillData SourceSkill { get; }

        public float RemainingDurationSeconds { get; private set; }

        public float TriggerRadius { get; }

        public float EffectRadius { get; }

        public float ForcedMovementDistance { get; }

        public float ForcedMovementDurationSeconds { get; }

        public float ForcedMovementPeakHeight { get; }

        public float HealProtectedHeroPerSuccessfulKnockUp { get; }

        public int TriggersRemaining { get; private set; }

        public System.Collections.Generic.IReadOnlyList<StatusEffectData> OnTriggerStatusEffects { get; }

        public bool IsExpired => RemainingDurationSeconds <= 0f || TriggersRemaining <= 0;

        public void Tick(float deltaTime)
        {
            RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - Mathf.Max(0f, deltaTime));
        }

        public void ConsumeTrigger()
        {
            TriggersRemaining = Mathf.Max(0, TriggersRemaining - 1);
            if (TriggersRemaining <= 0)
            {
                RemainingDurationSeconds = 0f;
            }
        }

        public void ExpireImmediately()
        {
            RemainingDurationSeconds = 0f;
            TriggersRemaining = 0;
        }
    }
}
