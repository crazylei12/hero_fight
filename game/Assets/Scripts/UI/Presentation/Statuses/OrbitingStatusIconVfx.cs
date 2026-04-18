using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.UI.Presentation.Statuses
{
    [DefaultExecutionOrder(1000)]
    public class OrbitingStatusIconVfx : MonoBehaviour
    {
        private enum OrbitMode
        {
            ScreenPlane = 0,
            BodyDepth = 1,
        }

        [SerializeField] private Transform orbitAnchor;
        [SerializeField] private float orbitSpeedDegreesPerSecond = 120f;
        [SerializeField] private bool keepAnchorUpright = true;
        [SerializeField] private bool randomizeStartingAngle = true;
        [SerializeField] private OrbitMode orbitMode = OrbitMode.ScreenPlane;
        [SerializeField] private Vector2 bodyOrbitRadius = new Vector2(0.28f, 0.1f);
        [SerializeField] private float backScaleMultiplier = 0.72f;
        [SerializeField] private float backAlphaMultiplier = 0.3f;
        [SerializeField] private int backSortingOrderOffset = -158;

        private Vector3 baseLocalPosition = Vector3.right * 0.35f;
        private Vector3 baseLocalScale = Vector3.one;
        private Quaternion baseLocalRotation = Quaternion.identity;
        private SpriteRenderer[] orbitRenderers = new SpriteRenderer[0];
        private Color[] baseRendererColors = new Color[0];
        private SortingGroup rootSortingGroup;
        private int baseSortingOrder;
        private int orbitSlotIndex;
        private int orbitSlotCount = 1;
        private float orbitStartPhaseDegrees;
        private float currentDepth = 1f;
        private bool initialized;

        public void Configure(
            Transform anchor,
            float orbitSpeedDegreesPerSecond,
            bool keepAnchorUpright,
            bool randomizeStartingAngle)
        {
            orbitAnchor = anchor;
            this.orbitSpeedDegreesPerSecond = orbitSpeedDegreesPerSecond;
            this.keepAnchorUpright = keepAnchorUpright;
            this.randomizeStartingAngle = randomizeStartingAngle;
            orbitMode = OrbitMode.ScreenPlane;
            CaptureBaseTransform();
            CaptureRendererState();
        }

        public void ConfigureBodyOrbit(
            Transform anchor,
            float orbitSpeedDegreesPerSecond,
            bool keepAnchorUpright,
            bool randomizeStartingAngle,
            Vector2 orbitRadius,
            float backScaleMultiplier,
            float backAlphaMultiplier,
            int backSortingOrderOffset)
        {
            orbitAnchor = anchor;
            this.orbitSpeedDegreesPerSecond = orbitSpeedDegreesPerSecond;
            this.keepAnchorUpright = keepAnchorUpright;
            this.randomizeStartingAngle = randomizeStartingAngle;
            orbitMode = OrbitMode.BodyDepth;
            bodyOrbitRadius = new Vector2(Mathf.Abs(orbitRadius.x), Mathf.Abs(orbitRadius.y));
            this.backScaleMultiplier = Mathf.Clamp(backScaleMultiplier, 0.1f, 1f);
            this.backAlphaMultiplier = Mathf.Clamp01(backAlphaMultiplier);
            this.backSortingOrderOffset = backSortingOrderOffset;
            CaptureBaseTransform();
            CaptureRendererState();
        }

        public void SetBaseSortingOrder(int sortingOrder)
        {
            baseSortingOrder = sortingOrder;
            ApplySortingOrder();
        }

        public void SetOrbitLayout(int slotIndex, int slotCount)
        {
            orbitSlotCount = Mathf.Max(1, slotCount);
            orbitSlotIndex = Mathf.Clamp(slotIndex, 0, orbitSlotCount - 1);
            ApplyOrbit();
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();
            ApplyOrbit();
        }

        private void Update()
        {
            if (!initialized || orbitAnchor == null)
            {
                return;
            }

            ApplyOrbit();
        }

        private void InitializeIfNeeded()
        {
            if (orbitAnchor == null)
            {
                orbitAnchor = transform.childCount > 0 ? transform.GetChild(0) : null;
            }

            if (orbitAnchor == null)
            {
                enabled = false;
                return;
            }

            CaptureBaseTransform();
            CaptureRendererState();
            if (!initialized && randomizeStartingAngle)
            {
                orbitStartPhaseDegrees = Random.Range(0f, 360f);
            }

            initialized = true;
        }

        private void CaptureBaseTransform()
        {
            if (orbitAnchor == null)
            {
                return;
            }

            baseLocalPosition = orbitAnchor.localPosition;
            baseLocalScale = orbitAnchor.localScale;
            baseLocalRotation = orbitAnchor.localRotation;
        }

        private void CaptureRendererState()
        {
            rootSortingGroup = GetComponent<SortingGroup>();
            baseSortingOrder = rootSortingGroup != null ? rootSortingGroup.sortingOrder : 0;
            if (orbitAnchor == null)
            {
                orbitRenderers = new SpriteRenderer[0];
                baseRendererColors = new Color[0];
                return;
            }

            orbitRenderers = orbitAnchor.GetComponentsInChildren<SpriteRenderer>(true);
            baseRendererColors = new Color[orbitRenderers.Length];
            for (var i = 0; i < orbitRenderers.Length; i++)
            {
                baseRendererColors[i] = orbitRenderers[i] != null
                    ? orbitRenderers[i].color
                    : Color.white;
            }
        }

        private void ApplyOrbit()
        {
            if (orbitAnchor == null)
            {
                return;
            }

            if (orbitMode == OrbitMode.BodyDepth)
            {
                ApplyBodyOrbit();
                return;
            }

            var orbitAngleDegrees = GetOrbitAngleDegrees();
            var orbitRotation = Quaternion.Euler(0f, 0f, orbitAngleDegrees);
            orbitAnchor.localPosition = orbitRotation * baseLocalPosition;
            orbitAnchor.localScale = baseLocalScale;
            orbitAnchor.localRotation = keepAnchorUpright
                ? Quaternion.Euler(0f, 0f, -orbitAngleDegrees) * baseLocalRotation
                : orbitRotation * baseLocalRotation;
            currentDepth = 1f;
            ApplyRendererAlpha(1f);
            ApplySortingOrder();
        }

        private void ApplyBodyOrbit()
        {
            var radians = GetOrbitAngleDegrees() * Mathf.Deg2Rad;
            var horizontal = Mathf.Sin(radians);
            currentDepth = Mathf.Cos(radians);
            var depthLerp = (currentDepth + 1f) * 0.5f;
            var multiIconScaleMultiplier = Mathf.Lerp(1f, 0.8f, Mathf.Clamp01((orbitSlotCount - 1) / 3f));

            orbitAnchor.localPosition = new Vector3(
                baseLocalPosition.x + (horizontal * bodyOrbitRadius.x),
                baseLocalPosition.y - (currentDepth * bodyOrbitRadius.y),
                baseLocalPosition.z);

            var scaleMultiplier = Mathf.Lerp(backScaleMultiplier, 1f, depthLerp);
            orbitAnchor.localScale = baseLocalScale * (scaleMultiplier * multiIconScaleMultiplier);
            orbitAnchor.localRotation = keepAnchorUpright
                ? baseLocalRotation
                : Quaternion.Euler(0f, 0f, -horizontal * 12f) * baseLocalRotation;
            ApplyRendererAlpha(Mathf.Lerp(backAlphaMultiplier, 1f, depthLerp));
            ApplySortingOrder();
        }

        private float GetOrbitAngleDegrees()
        {
            var sharedAngleDegrees = Time.time * orbitSpeedDegreesPerSecond;
            var slotPhaseDegrees = orbitSlotCount > 1
                ? 360f * orbitSlotIndex / orbitSlotCount
                : 0f;
            return Mathf.Repeat(sharedAngleDegrees + orbitStartPhaseDegrees + slotPhaseDegrees, 360f);
        }

        private void ApplySortingOrder()
        {
            if (rootSortingGroup == null)
            {
                return;
            }

            var sortingOffset = orbitMode == OrbitMode.BodyDepth && currentDepth < 0f
                ? backSortingOrderOffset
                : 0;
            rootSortingGroup.sortingOrder = baseSortingOrder + sortingOffset;
        }

        private void ApplyRendererAlpha(float alphaMultiplier)
        {
            if (orbitRenderers == null || baseRendererColors == null)
            {
                return;
            }

            var count = Mathf.Min(orbitRenderers.Length, baseRendererColors.Length);
            for (var i = 0; i < count; i++)
            {
                var renderer = orbitRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var color = baseRendererColors[i];
                color.a *= alphaMultiplier;
                renderer.color = color;
            }
        }
    }
}
