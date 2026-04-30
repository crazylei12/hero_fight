using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public sealed class RuntimeChanneledPathSkill
    {
        private static int nextChannelId;
        private float remainingChargeSeconds;
        private float remainingChannelSeconds;
        private float tickTimerSeconds;

        public RuntimeChanneledPathSkill(
            RuntimeHero caster,
            SkillData skill,
            SkillEffectData effect,
            RuntimeHero primaryTarget,
            Vector3 initialDirection)
        {
            Caster = caster;
            Skill = skill;
            Effect = effect;
            PrimaryTarget = primaryTarget;
            ChargeDurationSeconds = effect != null ? Mathf.Max(0f, effect.returningPathDelaySeconds) : 0f;
            ChannelDurationSeconds = effect != null ? Mathf.Max(0f, effect.durationSeconds) : 0f;
            TickIntervalSeconds = effect != null ? Mathf.Max(0.05f, effect.tickIntervalSeconds) : 1f;
            PathLength = effect != null ? Mathf.Max(0f, effect.returningPathMaxDistance) : 0f;
            PathWidth = effect != null ? Mathf.Max(0f, effect.returningPathWidth) : 0f;
            MaxTurnDegreesPerSecond = effect != null ? Mathf.Max(0f, effect.channeledPathMaxTurnDegreesPerSecond) : 0f;
            CurrentDirection = NormalizeFlatDirection(initialDirection, Vector3.forward);
            ExpectedTickCount = Mathf.Max(1, Mathf.CeilToInt(ChannelDurationSeconds / TickIntervalSeconds));
            remainingChargeSeconds = ChargeDurationSeconds;
            remainingChannelSeconds = ChannelDurationSeconds;
            tickTimerSeconds = TickIntervalSeconds;
            ChannelId = $"channeled_path_{nextChannelId++:D4}";
        }

        public string ChannelId { get; }

        public RuntimeHero Caster { get; }

        public SkillData Skill { get; }

        public SkillEffectData Effect { get; }

        public RuntimeHero PrimaryTarget { get; }

        public Vector3 CurrentDirection { get; private set; }

        public float ChargeDurationSeconds { get; }

        public float ChannelDurationSeconds { get; }

        public float TickIntervalSeconds { get; }

        public float PathLength { get; }

        public float PathWidth { get; }

        public float MaxTurnDegreesPerSecond { get; }

        public int ExpectedTickCount { get; }

        public int ResolvedTickCount { get; private set; }

        public bool IsComplete { get; private set; }

        public ChanneledPathEndReason EndReason { get; private set; } = ChanneledPathEndReason.Completed;

        public bool Advance(float deltaTime, Vector3 desiredDirection, out int pendingTickCount)
        {
            pendingTickCount = 0;
            if (IsComplete)
            {
                return false;
            }

            if (TryGetInterruptReason(out var interruptReason))
            {
                Complete(interruptReason);
                return false;
            }

            var step = Mathf.Max(0f, deltaTime);
            if (step <= Mathf.Epsilon)
            {
                return false;
            }

            TurnToward(desiredDirection, step);

            if (remainingChargeSeconds > Mathf.Epsilon)
            {
                var consumedCharge = Mathf.Min(remainingChargeSeconds, step);
                remainingChargeSeconds = Mathf.Max(0f, remainingChargeSeconds - consumedCharge);
                step -= consumedCharge;
                if (step <= Mathf.Epsilon)
                {
                    return false;
                }
            }

            if (remainingChannelSeconds <= Mathf.Epsilon)
            {
                Complete(ChanneledPathEndReason.Completed);
                return false;
            }

            var consumedChannel = Mathf.Min(remainingChannelSeconds, step);
            remainingChannelSeconds = Mathf.Max(0f, remainingChannelSeconds - consumedChannel);
            tickTimerSeconds -= consumedChannel;

            while (tickTimerSeconds <= Mathf.Epsilon && ResolvedTickCount + pendingTickCount < ExpectedTickCount)
            {
                pendingTickCount++;
                tickTimerSeconds += TickIntervalSeconds;
            }

            if (remainingChannelSeconds <= Mathf.Epsilon)
            {
                if (ResolvedTickCount + pendingTickCount < ExpectedTickCount)
                {
                    pendingTickCount++;
                }

                Complete(ChanneledPathEndReason.Completed);
            }

            return pendingTickCount > 0;
        }

        public void MarkTickResolved()
        {
            ResolvedTickCount++;
        }

        public void GetCurrentSegment(out Vector3 startPosition, out Vector3 endPosition)
        {
            startPosition = Caster != null ? Caster.CurrentPosition : Vector3.zero;
            startPosition.y = 0f;
            startPosition = Stage01ArenaSpec.ClampPosition(startPosition);
            endPosition = Stage01ArenaSpec.ClampPosition(startPosition + CurrentDirection * PathLength);
            endPosition.y = 0f;
        }

        private bool TryGetInterruptReason(out ChanneledPathEndReason reason)
        {
            if (Caster == null)
            {
                reason = ChanneledPathEndReason.CasterMissing;
                return true;
            }

            if (Caster.IsDead)
            {
                reason = ChanneledPathEndReason.CasterDead;
                return true;
            }

            if (Caster.HasHardControl)
            {
                reason = ChanneledPathEndReason.HardControl;
                return true;
            }

            if (Caster.IsUnderForcedMovement)
            {
                reason = ChanneledPathEndReason.ForcedMovement;
                return true;
            }

            reason = ChanneledPathEndReason.Completed;
            return false;
        }

        private void Complete(ChanneledPathEndReason reason)
        {
            IsComplete = true;
            EndReason = reason;
        }

        private void TurnToward(Vector3 desiredDirection, float deltaTime)
        {
            desiredDirection = NormalizeFlatDirection(desiredDirection, CurrentDirection);
            if (MaxTurnDegreesPerSecond <= Mathf.Epsilon)
            {
                return;
            }

            var maxRadiansDelta = MaxTurnDegreesPerSecond * Mathf.Deg2Rad * Mathf.Max(0f, deltaTime);
            CurrentDirection = Vector3.RotateTowards(CurrentDirection, desiredDirection, maxRadiansDelta, 0f);
            CurrentDirection = NormalizeFlatDirection(CurrentDirection, desiredDirection);
        }

        private static Vector3 NormalizeFlatDirection(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                return direction.normalized;
            }

            fallback.y = 0f;
            return fallback.sqrMagnitude > Mathf.Epsilon ? fallback.normalized : Vector3.forward;
        }
    }
}
