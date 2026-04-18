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
        private float currentAngleDegrees;
        private float currentDepth = 1f;
        private int lastAppliedSortingOrderOffset;
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

            currentAngleDegrees = Mathf.Repeat(
                currentAngleDegrees + (orbitSpeedDegreesPerSecond * Time.deltaTime),
                360f);
            ApplyOrbit();
        }

        private void LateUpdate()
        {
            if (!initialized || orbitMode != OrbitMode.BodyDepth || rootSortingGroup == null)
            {
                return;
            }

            var baseSortingOrder = rootSortingGroup.sortingOrder - lastAppliedSortingOrderOffset;
            lastAppliedSortingOrderOffset = currentDepth < 0f
                ? backSortingOrderOffset
                : 0;
            rootSortingGroup.sortingOrder = baseSortingOrder + lastAppliedSortingOrderOffset;
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
                currentAngleDegrees = Random.Range(0f, 360f);
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

            var orbitRotation = Quaternion.Euler(0f, 0f, currentAngleDegrees);
            orbitAnchor.localPosition = orbitRotation * baseLocalPosition;
            orbitAnchor.localScale = baseLocalScale;
            orbitAnchor.localRotation = keepAnchorUpright
                ? Quaternion.Euler(0f, 0f, -currentAngleDegrees) * baseLocalRotation
                : orbitRotation * baseLocalRotation;
            currentDepth = 1f;
            ApplyRendererAlpha(1f);
        }

        private void ApplyBodyOrbit()
        {
            var radians = currentAngleDegrees * Mathf.Deg2Rad;
            var horizontal = Mathf.Sin(radians);
            currentDepth = Mathf.Cos(radians);
            var depthLerp = (currentDepth + 1f) * 0.5f;

            orbitAnchor.localPosition = new Vector3(
                baseLocalPosition.x + (horizontal * bodyOrbitRadius.x),
                baseLocalPosition.y - (currentDepth * bodyOrbitRadius.y),
                baseLocalPosition.z);

            var scaleMultiplier = Mathf.Lerp(backScaleMultiplier, 1f, depthLerp);
            orbitAnchor.localScale = baseLocalScale * scaleMultiplier;
            orbitAnchor.localRotation = keepAnchorUpright
                ? baseLocalRotation
                : Quaternion.Euler(0f, 0f, -horizontal * 12f) * baseLocalRotation;
            ApplyRendererAlpha(Mathf.Lerp(backAlphaMultiplier, 1f, depthLerp));
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
