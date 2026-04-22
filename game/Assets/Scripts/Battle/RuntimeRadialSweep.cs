using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public sealed class RuntimeRadialSweep
    {
        private static int nextSweepId;
        private readonly HashSet<string> hitTargetIds = new HashSet<string>();
        private bool skipFirstTick = true;

        public RuntimeRadialSweep(RuntimeHero caster, SkillData skill, SkillEffectData effect, Vector3 center)
        {
            Caster = caster;
            Skill = skill;
            Effect = effect;
            Center = new Vector3(center.x, 0f, center.z);
            Direction = effect != null ? effect.radialSweepDirection : RadialSweepDirectionMode.Outward;
            MaxRadius = effect != null && effect.radiusOverride > 0f
                ? effect.radiusOverride
                : skill != null
                    ? Mathf.Max(0f, skill.areaRadius)
                    : 0f;
            RingWidth = effect != null ? Mathf.Max(0.1f, effect.radialSweepRingWidth) : 1f;
            RemainingStartDelaySeconds = effect != null ? Mathf.Max(0f, effect.radialSweepStartDelaySeconds) : 0f;
            TotalDurationSeconds = effect != null ? Mathf.Max(0.0001f, effect.durationSeconds) : 0.0001f;
            SweepId = $"radial_sweep_{nextSweepId++:D4}";
            CurrentRadius = GetRadiusAtProgress(0f);
        }

        public string SweepId { get; }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public SkillEffectData Effect { get; }

        public Vector3 Center { get; }

        public RadialSweepDirectionMode Direction { get; }

        public float MaxRadius { get; }

        public float RingWidth { get; }

        public float RemainingStartDelaySeconds { get; private set; }

        public float TotalDurationSeconds { get; }

        public float ElapsedSeconds { get; private set; }

        public float CurrentRadius { get; private set; }

        public int HitCount => hitTargetIds.Count;

        public bool IsComplete { get; private set; }

        public bool Advance(float deltaTime, out float segmentInnerRadius, out float segmentOuterRadius)
        {
            segmentInnerRadius = 0f;
            segmentOuterRadius = 0f;

            if (IsComplete)
            {
                return false;
            }

            if (skipFirstTick)
            {
                skipFirstTick = false;
                return false;
            }

            var remainingDeltaSeconds = Mathf.Max(0f, deltaTime);
            if (RemainingStartDelaySeconds > 0f)
            {
                var consumedDelaySeconds = Mathf.Min(RemainingStartDelaySeconds, remainingDeltaSeconds);
                RemainingStartDelaySeconds -= consumedDelaySeconds;
                remainingDeltaSeconds -= consumedDelaySeconds;
                if (RemainingStartDelaySeconds > Mathf.Epsilon || remainingDeltaSeconds <= Mathf.Epsilon)
                {
                    return false;
                }
            }

            var previousRadius = GetRadiusAtElapsed(ElapsedSeconds);
            ElapsedSeconds = Mathf.Min(TotalDurationSeconds, ElapsedSeconds + remainingDeltaSeconds);
            CurrentRadius = GetRadiusAtElapsed(ElapsedSeconds);

            var halfWidth = RingWidth * 0.5f;
            segmentInnerRadius = Mathf.Max(0f, Mathf.Min(previousRadius, CurrentRadius) - halfWidth);
            segmentOuterRadius = Mathf.Min(MaxRadius + halfWidth, Mathf.Max(previousRadius, CurrentRadius) + halfWidth);

            if (ElapsedSeconds >= TotalDurationSeconds - Mathf.Epsilon)
            {
                IsComplete = true;
            }

            return segmentOuterRadius > segmentInnerRadius + Mathf.Epsilon;
        }

        public bool TryRegisterHit(RuntimeHero target)
        {
            return target != null
                && !string.IsNullOrWhiteSpace(target.RuntimeId)
                && hitTargetIds.Add(target.RuntimeId);
        }

        private float GetRadiusAtElapsed(float elapsedSeconds)
        {
            var progress = TotalDurationSeconds > Mathf.Epsilon
                ? Mathf.Clamp01(elapsedSeconds / TotalDurationSeconds)
                : 1f;
            return GetRadiusAtProgress(progress);
        }

        private float GetRadiusAtProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return Direction == RadialSweepDirectionMode.Inward
                ? MaxRadius * (1f - progress)
                : MaxRadius * progress;
        }
    }
}
