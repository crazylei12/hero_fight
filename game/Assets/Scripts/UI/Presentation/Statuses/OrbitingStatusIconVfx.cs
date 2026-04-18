using UnityEngine;

namespace Fight.UI.Presentation.Statuses
{
    public class OrbitingStatusIconVfx : MonoBehaviour
    {
        [SerializeField] private Transform orbitAnchor;
        [SerializeField] private float orbitSpeedDegreesPerSecond = 120f;
        [SerializeField] private bool keepAnchorUpright = true;
        [SerializeField] private bool randomizeStartingAngle = true;

        private Vector3 baseLocalPosition = Vector3.right * 0.35f;
        private Quaternion baseLocalRotation = Quaternion.identity;
        private float currentAngleDegrees;
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
            CaptureBaseTransform();
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
            baseLocalRotation = orbitAnchor.localRotation;
        }

        private void ApplyOrbit()
        {
            if (orbitAnchor == null)
            {
                return;
            }

            var orbitRotation = Quaternion.Euler(0f, 0f, currentAngleDegrees);
            orbitAnchor.localPosition = orbitRotation * baseLocalPosition;
            orbitAnchor.localRotation = keepAnchorUpright
                ? Quaternion.Euler(0f, 0f, -currentAngleDegrees) * baseLocalRotation
                : orbitRotation * baseLocalRotation;
        }
    }
}
