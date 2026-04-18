using Fight.Core;
using UnityEngine;

namespace Fight.Heroes
{
    public sealed class RuntimeForcedMovement
    {
        private readonly Vector3 startPosition;
        private readonly Vector3 endPosition;

        public RuntimeForcedMovement(Vector3 startPosition, Vector3 endPosition, float durationSeconds, float peakHeight)
        {
            this.startPosition = startPosition;
            this.endPosition = endPosition;
            TotalDurationSeconds = Mathf.Max(0f, durationSeconds);
            RemainingDurationSeconds = TotalDurationSeconds;
            PeakHeight = Mathf.Max(0f, peakHeight);
            CurrentGroundPosition = startPosition;
            CurrentHeightOffset = 0f;

            if (TotalDurationSeconds <= Mathf.Epsilon)
            {
                CurrentGroundPosition = endPosition;
            }
        }

        public float TotalDurationSeconds { get; }

        public float RemainingDurationSeconds { get; private set; }

        public float PeakHeight { get; }

        public Vector3 CurrentGroundPosition { get; private set; }

        public float CurrentHeightOffset { get; private set; }

        public bool IsComplete => RemainingDurationSeconds <= 0f;

        public void Tick(float deltaTime)
        {
            if (IsComplete)
            {
                CurrentGroundPosition = endPosition;
                CurrentHeightOffset = 0f;
                return;
            }

            RemainingDurationSeconds = Mathf.Max(0f, RemainingDurationSeconds - Mathf.Max(0f, deltaTime));
            var elapsedSeconds = TotalDurationSeconds - RemainingDurationSeconds;
            var progress = TotalDurationSeconds > Mathf.Epsilon
                ? Mathf.Clamp01(elapsedSeconds / TotalDurationSeconds)
                : 1f;

            CurrentGroundPosition = Vector3.Lerp(startPosition, endPosition, progress);
            CurrentHeightOffset = ParabolicMotionUtility.EvaluateHeightOffset(progress, PeakHeight);
        }
    }
}
