using Fight.Battle;
using Fight.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.UI.Presentation.Skills
{
    public sealed class ThrownProjectileSkillAreaPresentationController : SkillAreaPresentationController
    {
        private const float DefaultArcHeight = 0.58f;
        private const float MinFlightDurationSeconds = 0.1f;
        private const float SpinDegreesPerFlight = -600f;

        private SortingGroup sortingGroup;
        private GameObject projectileInstance;
        private Renderer[] renderers = System.Array.Empty<Renderer>();
        private Vector3 launchPosition;
        private Vector3 currentTargetPosition;
        private Vector3 projectileBaseScale = Vector3.one;
        private float flightDurationSeconds = MinFlightDurationSeconds;
        private bool launchInitialized;
        private bool pulseStarted;
        private bool firstRestartConsumed;

        public override Renderer[] Renderers => renderers;

        public override bool UsesWorldSorting => true;

        public void Initialize(GameObject projectilePrefab, Vector3 startPosition, float flightDurationSeconds)
        {
            sortingGroup = gameObject.GetComponent<SortingGroup>();
            if (sortingGroup == null)
            {
                sortingGroup = gameObject.AddComponent<SortingGroup>();
            }

            launchPosition = startPosition;
            currentTargetPosition = startPosition;
            this.flightDurationSeconds = Mathf.Max(MinFlightDurationSeconds, flightDurationSeconds);
            launchInitialized = true;

            if (projectilePrefab == null)
            {
                return;
            }

            projectileInstance = Instantiate(projectilePrefab, transform);
            projectileInstance.name = "ThrownProjectile";
            projectileInstance.transform.localPosition = Vector3.zero;
            projectileBaseScale = projectileInstance.transform.localScale;
            renderers = projectileInstance.GetComponentsInChildren<Renderer>(true);
        }

        public override Vector3 GetScaledSize(RuntimeSkillArea area, Vector3 defaultAreaScale)
        {
            return Vector3.one;
        }

        public override Vector3 GetWorldSortingPosition(Vector3 defaultPosition)
        {
            if (projectileInstance != null && projectileInstance.activeInHierarchy)
            {
                return projectileInstance.transform.position;
            }

            return launchInitialized ? currentTargetPosition : defaultPosition;
        }

        public override void Sync(RuntimeSkillArea area, Vector3 position, int sortingOrder, float expiryFadeSeconds)
        {
            currentTargetPosition = position;
            if (sortingGroup != null)
            {
                sortingGroup.sortingOrder = sortingOrder;
            }

            if (!launchInitialized || projectileInstance == null)
            {
                return;
            }

            if (pulseStarted)
            {
                projectileInstance.SetActive(false);
                return;
            }

            projectileInstance.SetActive(true);
            var totalDuration = Mathf.Max(MinFlightDurationSeconds, area != null ? area.TotalDurationSeconds : flightDurationSeconds);
            var remainingDuration = Mathf.Clamp(area != null ? area.RemainingDurationSeconds : 0f, 0f, totalDuration);
            var progress = Mathf.Clamp01(1f - (remainingDuration / totalDuration));
            var worldPosition = Vector3.Lerp(launchPosition, currentTargetPosition, progress);
            worldPosition.y += ParabolicMotionUtility.EvaluateHeightOffset(progress, DefaultArcHeight);
            projectileInstance.transform.position = worldPosition;

            var direction = currentTargetPosition - worldPosition;
            if (direction.sqrMagnitude > 0.0001f)
            {
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                projectileInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f + (progress * SpinDegreesPerFlight));
            }
            else
            {
                projectileInstance.transform.rotation = Quaternion.Euler(0f, 0f, progress * SpinDegreesPerFlight);
            }

            projectileInstance.transform.localScale = projectileBaseScale;
        }

        public override void RestartPulse()
        {
            if (!firstRestartConsumed)
            {
                firstRestartConsumed = true;
                return;
            }

            pulseStarted = true;
            if (projectileInstance != null)
            {
                projectileInstance.SetActive(false);
            }
        }

        public override void Cleanup()
        {
        }
    }
}
