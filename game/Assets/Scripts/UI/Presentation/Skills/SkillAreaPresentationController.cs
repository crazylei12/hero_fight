using Fight.Battle;
using UnityEngine;

namespace Fight.UI.Presentation.Skills
{
    public abstract class SkillAreaPresentationController : MonoBehaviour
    {
        public abstract Renderer[] Renderers { get; }

        public abstract Vector3 GetScaledSize(RuntimeSkillArea area, Vector3 defaultAreaScale);

        public abstract void Sync(RuntimeSkillArea area, Vector3 position, int sortingOrder, float expiryFadeSeconds);

        public abstract void RestartPulse();

        public abstract void Cleanup();
    }
}
