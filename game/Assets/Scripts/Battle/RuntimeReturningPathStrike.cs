using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public sealed class RuntimeReturningPathStrike
    {
        private static int nextStrikeId;
        private float remainingDelaySeconds;
        private float remainingTravelSeconds;

        public RuntimeReturningPathStrike(
            RuntimeHero caster,
            SkillData skill,
            SkillEffectData effect,
            Vector3 startPosition,
            Vector3 endPosition,
            float pathWidth)
        {
            Caster = caster;
            Skill = skill;
            Effect = effect;
            Phase = effect != null ? effect.returningPathStrikePhase : ReturningPathStrikePhase.Outbound;
            StartPosition = startPosition;
            EndPosition = endPosition;
            PathWidth = Mathf.Max(0f, pathWidth);
            DelaySeconds = effect != null ? Mathf.Max(0f, effect.returningPathDelaySeconds) : 0f;
            TravelDurationSeconds = effect != null ? Mathf.Max(0f, effect.durationSeconds) : 0f;
            remainingDelaySeconds = DelaySeconds;
            remainingTravelSeconds = TravelDurationSeconds;
            StrikeId = $"returning_path_{nextStrikeId++:D4}";
        }

        public string StrikeId { get; }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public SkillEffectData Effect { get; }

        public ReturningPathStrikePhase Phase { get; }

        public Vector3 StartPosition { get; }

        public Vector3 EndPosition { get; }

        public float PathWidth { get; }

        public float DelaySeconds { get; }

        public float TravelDurationSeconds { get; }

        public bool IsComplete { get; private set; }

        public bool IsImmediate => DelaySeconds <= Mathf.Epsilon && TravelDurationSeconds <= Mathf.Epsilon;

        public bool Advance(float deltaTime)
        {
            if (IsComplete)
            {
                return false;
            }

            var remainingStep = Mathf.Max(0f, deltaTime);
            if (remainingStep <= Mathf.Epsilon)
            {
                return false;
            }

            if (remainingDelaySeconds > Mathf.Epsilon)
            {
                var consumedDelay = Mathf.Min(remainingDelaySeconds, remainingStep);
                remainingDelaySeconds = Mathf.Max(0f, remainingDelaySeconds - consumedDelay);
                remainingStep -= consumedDelay;
                if (remainingStep <= Mathf.Epsilon)
                {
                    return false;
                }
            }

            if (remainingTravelSeconds > Mathf.Epsilon)
            {
                var consumedTravel = Mathf.Min(remainingTravelSeconds, remainingStep);
                remainingTravelSeconds = Mathf.Max(0f, remainingTravelSeconds - consumedTravel);
            }

            if (remainingDelaySeconds > Mathf.Epsilon || remainingTravelSeconds > Mathf.Epsilon)
            {
                return false;
            }

            IsComplete = true;
            return true;
        }

        public void CompleteImmediately()
        {
            remainingDelaySeconds = 0f;
            remainingTravelSeconds = 0f;
            IsComplete = true;
        }
    }
}
