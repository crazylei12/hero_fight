using UnityEngine;

namespace Fight.UI.Presentation.Skills
{
    public sealed class LoopingRotationVfx : MonoBehaviour
    {
        [SerializeField] private Vector3 localEulerDegreesPerSecond = new Vector3(0f, 0f, -720f);

        public void Configure(Vector3 eulerDegreesPerSecond)
        {
            localEulerDegreesPerSecond = eulerDegreesPerSecond;
        }

        private void Update()
        {
            if (localEulerDegreesPerSecond == Vector3.zero)
            {
                return;
            }

            transform.localRotation *= Quaternion.Euler(localEulerDegreesPerSecond * Time.deltaTime);
        }
    }
}
