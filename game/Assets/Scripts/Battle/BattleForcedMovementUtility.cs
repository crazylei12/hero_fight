using System.Collections.Generic;
using Fight.Core;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    internal static class BattleForcedMovementUtility
    {
        private const float TowardSourceStopDistance = 1.15f;
        private const float TowardSourceSpreadMinDistance = 1.2f;
        private const float TowardSourceSpreadMaxDistance = 2.2f;
        private const float TowardSourceSpreadRingStep = 0.35f;
        private const float TowardSourceSpreadPositionTolerance = 0.05f;

        public static void ApplyForcedMovementToTargets(
            BattleContext context,
            RuntimeHero source,
            Vector3 anchorPosition,
            SkillData sourceSkill,
            SkillEffectData effect,
            IReadOnlyList<RuntimeHero> targets)
        {
            if (effect == null || targets == null || targets.Count <= 0)
            {
                return;
            }

            var distance = Mathf.Max(0f, effect.forcedMovementDistance);
            var durationSeconds = Mathf.Max(0f, effect.forcedMovementDurationSeconds);
            var peakHeight = Mathf.Max(0f, effect.forcedMovementPeakHeight);
            if (distance <= Mathf.Epsilon && durationSeconds <= Mathf.Epsilon)
            {
                return;
            }

            anchorPosition = Stage01ArenaSpec.ClampPosition(anchorPosition);
            anchorPosition.y = 0f;

            var spreadDestinations = ResolveTowardSourceSpreadDestinations(anchorPosition, effect, targets, distance);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || target.IsDead)
                {
                    continue;
                }

                var startPosition = target.CurrentPosition;
                var destination = spreadDestinations.TryGetValue(target.RuntimeId, out var assignedDestination)
                    ? assignedDestination
                    : GetForcedMovementDestination(anchorPosition, target, effect, distance);
                target.StartForcedMovement(destination, durationSeconds, peakHeight);

                if (source != null)
                {
                    BattleStatsSystem.RecordForcedMovementContribution(context, source, target);
                }

                context?.EventBus?.Publish(new ForcedMovementAppliedEvent(
                    source,
                    target,
                    startPosition,
                    destination,
                    durationSeconds,
                    peakHeight,
                    sourceSkill));
            }
        }

        private static Dictionary<string, Vector3> ResolveTowardSourceSpreadDestinations(
            Vector3 anchorPosition,
            SkillEffectData effect,
            IReadOnlyList<RuntimeHero> targets,
            float maxTravelDistance)
        {
            var results = new Dictionary<string, Vector3>();
            if (effect == null
                || effect.forcedMovementDirection != ForcedMovementDirectionMode.TowardSource
                || targets == null)
            {
                return results;
            }

            var validTargets = new List<RuntimeHero>();
            var seenTargetIds = new HashSet<string>();
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || target.IsDead || !seenTargetIds.Add(target.RuntimeId))
                {
                    continue;
                }

                validTargets.Add(target);
            }

            if (validTargets.Count <= 1)
            {
                return results;
            }

            var candidates = BuildTowardSourceSpreadCandidates(anchorPosition, validTargets.Count);
            if (candidates.Count <= 0)
            {
                return results;
            }

            validTargets.Sort((first, second) =>
                GetTowardSourcePreferredAngle(anchorPosition, first).CompareTo(GetTowardSourcePreferredAngle(anchorPosition, second)));

            var assignedPositions = new List<Vector3>();
            var candidateTaken = new bool[candidates.Count];
            for (var i = 0; i < validTargets.Count; i++)
            {
                var target = validTargets[i];
                var bestCandidateIndex = FindBestTowardSourceSpreadCandidate(
                    anchorPosition,
                    target,
                    candidates,
                    candidateTaken,
                    assignedPositions,
                    maxTravelDistance);
                if (bestCandidateIndex < 0)
                {
                    continue;
                }

                candidateTaken[bestCandidateIndex] = true;
                var destination = candidates[bestCandidateIndex];
                assignedPositions.Add(destination);
                results[target.RuntimeId] = destination;
            }

            return results;
        }

        private static List<Vector3> BuildTowardSourceSpreadCandidates(Vector3 anchorPosition, int targetCount)
        {
            var results = new List<Vector3>();
            var minimumTargetCount = Mathf.Max(1, targetCount);
            for (var radius = TowardSourceSpreadMinDistance;
                radius <= TowardSourceSpreadMaxDistance + Mathf.Epsilon;
                radius += TowardSourceSpreadRingStep)
            {
                var circumference = 2f * Mathf.PI * Mathf.Max(radius, 0.01f);
                var slots = Mathf.Max(minimumTargetCount, Mathf.CeilToInt(circumference / Mathf.Max(0.01f, Stage01ArenaSpec.UnitMinimumSeparationWorldUnits)));
                for (var i = 0; i < slots; i++)
                {
                    var angle = (Mathf.PI * 2f * i) / slots;
                    var candidate = anchorPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    candidate = Stage01ArenaSpec.ClampPosition(candidate);
                    candidate.y = 0f;

                    var candidateRadius = Vector3.Distance(anchorPosition, candidate);
                    if (candidateRadius < TowardSourceSpreadMinDistance - TowardSourceSpreadPositionTolerance
                        || candidateRadius > TowardSourceSpreadMaxDistance + TowardSourceSpreadPositionTolerance
                        || ContainsNearbyPosition(results, candidate, TowardSourceSpreadPositionTolerance))
                    {
                        continue;
                    }

                    results.Add(candidate);
                }
            }

            return results;
        }

        private static int FindBestTowardSourceSpreadCandidate(
            Vector3 anchorPosition,
            RuntimeHero target,
            IReadOnlyList<Vector3> candidates,
            IReadOnlyList<bool> candidateTaken,
            IReadOnlyList<Vector3> assignedPositions,
            float maxTravelDistance)
        {
            var bestCandidateIndex = -1;
            var bestScore = float.MaxValue;
            var preferredAngle = GetTowardSourcePreferredAngle(anchorPosition, target);
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidateTaken[i])
                {
                    continue;
                }

                var candidate = candidates[i];
                if (!IsTowardSourceSpreadCandidateValid(anchorPosition, target, candidate, maxTravelDistance, assignedPositions))
                {
                    continue;
                }

                var angle = GetAngle(anchorPosition, candidate);
                var angleDelta = Mathf.Abs(Mathf.DeltaAngle(preferredAngle, angle));
                var radiusPenalty = Mathf.Abs(Vector3.Distance(anchorPosition, candidate) - TowardSourceSpreadMinDistance) * 5f;
                var score = angleDelta + radiusPenalty;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestCandidateIndex = i;
            }

            return bestCandidateIndex;
        }

        private static bool IsTowardSourceSpreadCandidateValid(
            Vector3 anchorPosition,
            RuntimeHero target,
            Vector3 candidate,
            float maxTravelDistance,
            IReadOnlyList<Vector3> assignedPositions)
        {
            var currentSeparation = Vector3.Distance(target.CurrentPosition, anchorPosition);
            var candidateSeparation = Vector3.Distance(candidate, anchorPosition);
            if (candidateSeparation > currentSeparation + TowardSourceSpreadPositionTolerance)
            {
                return false;
            }

            if (maxTravelDistance > Mathf.Epsilon
                && Vector3.Distance(target.CurrentPosition, candidate) > maxTravelDistance + TowardSourceSpreadPositionTolerance)
            {
                return false;
            }

            for (var i = 0; i < assignedPositions.Count; i++)
            {
                if (Vector3.Distance(assignedPositions[i], candidate)
                    < Mathf.Max(0.01f, Stage01ArenaSpec.UnitMinimumSeparationWorldUnits - TowardSourceSpreadPositionTolerance))
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector3 GetForcedMovementDestination(
            Vector3 anchorPosition,
            RuntimeHero target,
            SkillEffectData effect,
            float distance)
        {
            var directionMode = effect != null ? effect.forcedMovementDirection : ForcedMovementDirectionMode.AwayFromSource;
            var direction = GetForcedMovementDirection(anchorPosition, target, directionMode);
            if (target == null)
            {
                return Vector3.zero;
            }

            if (directionMode == ForcedMovementDirectionMode.TowardSource)
            {
                var toAnchor = anchorPosition - target.CurrentPosition;
                toAnchor.y = 0f;
                var separation = toAnchor.magnitude;
                if (separation <= Mathf.Epsilon)
                {
                    return Stage01ArenaSpec.ClampPosition(target.CurrentPosition);
                }

                var maxTravelDistance = Mathf.Max(0f, separation - TowardSourceStopDistance);
                var towardTravelDistance = distance > Mathf.Epsilon
                    ? Mathf.Min(distance, maxTravelDistance)
                    : maxTravelDistance;
                var towardDestination = target.CurrentPosition + direction * towardTravelDistance;
                towardDestination.y = 0f;
                return Stage01ArenaSpec.ClampPosition(towardDestination);
            }

            var destination = target.CurrentPosition + direction * distance;
            destination.y = 0f;
            return Stage01ArenaSpec.ClampPosition(destination);
        }

        private static Vector3 GetForcedMovementDirection(
            Vector3 anchorPosition,
            RuntimeHero target,
            ForcedMovementDirectionMode directionMode)
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            var offset = directionMode == ForcedMovementDirectionMode.TowardSource
                ? anchorPosition - target.CurrentPosition
                : target.CurrentPosition - anchorPosition;
            offset.y = 0f;

            if (offset.sqrMagnitude > Mathf.Epsilon)
            {
                return offset.normalized;
            }

            return target.Side == TeamSide.Blue ? Vector3.left : Vector3.right;
        }

        private static float GetTowardSourcePreferredAngle(Vector3 anchorPosition, RuntimeHero target)
        {
            return GetAngle(anchorPosition, target != null ? target.CurrentPosition : anchorPosition);
        }

        private static float GetAngle(Vector3 anchorPosition, Vector3 position)
        {
            var direction = position - anchorPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            return Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        }

        private static bool ContainsNearbyPosition(IReadOnlyList<Vector3> positions, Vector3 candidate, float tolerance)
        {
            for (var i = 0; i < positions.Count; i++)
            {
                if (Vector3.Distance(positions[i], candidate) <= tolerance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
